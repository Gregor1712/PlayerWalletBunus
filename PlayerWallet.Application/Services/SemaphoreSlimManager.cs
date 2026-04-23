using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using PlayerWallet.Application.Interfaces;

namespace PlayerWallet.Application.Services;

// https://stackoverflow.com/questions/31138179/asynchronous-locking-based-on-a-key

// Táto trieda zabezpečuje, že pre jedného hráča (playerId) môže naraz prebiehať len jedna operácia.
// Funguje to ako "zámok na kľúč" – každý hráč má svoj vlastný zámok.
// Ak prídu dve požiadavky pre toho istého hráča súčasne, druhá počká, kým prvá skončí.
public class SemaphoreSlimManager : ISemaphoreSlimManager
{
    // Slovník (mapa), kde kľúč je ID hráča a hodnota je jeho zámok (semafór).
    // ConcurrentDictionary je bezpečný pre viacero vlákien naraz – viacero požiadaviek môže pristupovať súčasne.
    private readonly ConcurrentDictionary<Guid, RefCountedSemaphore> _locks = new();
    private readonly ILogger<SemaphoreSlimManager> _logger;

    // Počet aktuálne aktívnych zámkov (pre koľkých hráčov momentálne existuje zámok).
    public int Count => _locks.Count;

    public SemaphoreSlimManager(ILogger<SemaphoreSlimManager> logger)
    {
        _logger = logger;
    }

    // Hlavná metóda – zamkne prístup pre daného hráča.
    // Kým je zámok aktívny, žiadna iná operácia pre tohto hráča neprejde.
    // Po skončení práce sa zámok automaticky uvoľní vďaka "await using" (IAsyncDisposable).
    public async Task<IAsyncDisposable> LockAsync(Guid playerId, CancellationToken cancellationToken = default)
    {
        // Krok 1: Získaj (alebo vytvor) zámok pre tohto hráča a zvýš počítadlo používateľov.
        var entry = Acquire(playerId);

        // Krok 2: Počkaj, kým bude zámok voľný (ak ho práve používa iná požiadavka, čakáme tu).
        await entry.Semaphore.WaitAsync(cancellationToken);
        _logger.LogDebug("Lock acquired for player {PlayerId}", playerId);

        // Krok 3: Vráť objekt "Releaser", ktorý po zavolaní DisposeAsync uvoľní zámok.
        // To sa stane automaticky na konci bloku "await using".
        return new Releaser(() => Release(playerId, entry));
    }

    // Získanie zámku pre hráča – ak ešte neexistuje, vytvorí nový.
    // Ak existuje, zvýši počítadlo (RefCount), aby sme vedeli, koľko požiadaviek ho práve používa.
    private RefCountedSemaphore Acquire(Guid playerId)
    {
        // Nekonečný cyklus – opakujeme, kým sa nám nepodarí získať platný (nevyhodený) zámok.
        while (true)
        {
            // Skús nájsť existujúci zámok pre hráča. Ak neexistuje, vytvor nový.
            var entry = _locks.GetOrAdd(playerId, _ =>
            {
                _logger.LogDebug("Creating new semaphore for player {PlayerId}", playerId);
                return new RefCountedSemaphore();
            });

            // Zamkneme prístup k samotnému objektu zámku, aby sme bezpečne zvýšili počítadlo.
            lock (entry)
            {
                // Ak bol tento zámok medzičasom vyhodený (uvoľnený inou požiadavkou),
                // musíme skúsiť znova – preto ten while(true) cyklus.
                if (entry.IsEvicted)
                    continue;

                // Zvýšime počítadlo – "ešte jedna požiadavka tento zámok používa".
                entry.RefCount++;
                return entry;
            }
        }
    }

    // Uvoľnenie zámku po dokončení operácie.
    private void Release(Guid playerId, RefCountedSemaphore entry)
    {
        // Uvoľni semafór – pustí ďalšiu čakajúcu požiadavku (ak nejaká čaká).
        entry.Semaphore.Release();

        // Bezpečne znížime počítadlo používateľov tohto zámku.
        lock (entry)
        {
            entry.RefCount--;
            _logger.LogDebug("Semaphore released for player {PlayerId}, RefCount={RefCount}", playerId, entry.RefCount);

            // Ak už nikto tento zámok nepoužíva (počítadlo je 0), upraceme po sebe:
            if (entry.RefCount <= 0)
            {
                // Označíme zámok ako vyhodený, aby ho nikto nový nezískal.
                entry.IsEvicted = true;
                // Odstránime ho zo slovníka.
                _locks.TryRemove(playerId, out _);
                // Uvoľníme systémové prostriedky semafóru.
                entry.Semaphore.Dispose();
                _logger.LogDebug("Semaphore evicted and disposed for player {PlayerId}, ActiveLocks={Count}", playerId, _locks.Count);
            }
        }
    }

    // Interná trieda – zámok s počítadlom koľko požiadaviek ho práve používa.
    private class RefCountedSemaphore
    {
        // Samotný semafór – pustí naraz len 1 požiadavku (preto parametre 1, 1).
        public SemaphoreSlim Semaphore { get; } = new(1, 1);
        // Počítadlo – koľko požiadaviek momentálne drží referenciu na tento zámok.
        public int RefCount { get; set; }
        // Príznak, či bol zámok už vyhodený zo slovníka (uvoľnený).
        public bool IsEvicted { get; set; }
    }
        
    // Pomocná trieda – zabezpečuje automatické uvoľnenie zámku.
    // Keď skončí blok "await using", zavolá sa DisposeAsync, ktorý spustí uvoľňovaciu akciu.
    private class Releaser(Action onDispose) : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            // Zavolaj akciu na uvoľnenie (Release metódu vyššie).
            onDispose();
            return ValueTask.CompletedTask;
        }
    }
}