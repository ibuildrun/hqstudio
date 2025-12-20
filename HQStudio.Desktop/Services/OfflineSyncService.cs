using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Timer = System.Timers.Timer;

namespace HQStudio.Services;

/// <summary>
/// Сервис для работы в оффлайн режиме и синхронизации данных
/// </summary>
public class OfflineSyncService : IDisposable
{
    private readonly ApiService _api;
    private readonly string _offlineDataPath;
    private readonly Timer _syncTimer;
    private readonly Timer _heartbeatTimer;
    private readonly Timer _connectionCheckTimer;
    
    private bool _isOnline = true;
    private int _sessionId;
    private readonly List<PendingCallback> _pendingCallbacks = new();
    
    public event EventHandler<bool>? ConnectionStatusChanged;
    public event EventHandler<string>? SyncStatusChanged;
    public event EventHandler<int>? PendingCountChanged;
    
    public bool IsOnline => _isOnline;
    public int PendingCount => _pendingCallbacks.Count;

    public OfflineSyncService(ApiService api)
    {
        _api = api;
        _offlineDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "HQStudio", "offline_data.json");
        
        Directory.CreateDirectory(Path.GetDirectoryName(_offlineDataPath)!);
        
        // Загружаем несинхронизированные данные
        LoadPendingData();
        
        // Таймер синхронизации (каждые 30 сек)
        _syncTimer = new Timer(30000);
        _syncTimer.Elapsed += async (s, e) => await TrySyncPendingData();
        
        // Таймер heartbeat (каждые 20 сек)
        _heartbeatTimer = new Timer(20000);
        _heartbeatTimer.Elapsed += async (s, e) => await SendHeartbeat();
        
        // Таймер проверки соединения (каждые 10 сек)
        _connectionCheckTimer = new Timer(10000);
        _connectionCheckTimer.Elapsed += async (s, e) => await CheckConnection();
    }

    public void Start(int sessionId)
    {
        _sessionId = sessionId;
        _syncTimer.Start();
        _heartbeatTimer.Start();
        _connectionCheckTimer.Start();
    }

    public void Stop()
    {
        _syncTimer.Stop();
        _heartbeatTimer.Stop();
        _connectionCheckTimer.Stop();
    }

    /// <summary>
    /// Добавить заявку (сохраняется локально если оффлайн)
    /// </summary>
    public async Task<bool> AddCallbackAsync(CallbackData callback)
    {
        if (_isOnline)
        {
            try
            {
                var result = await _api.CreateCallbackAsync(callback);
                if (result != null)
                {
                    SyncStatusChanged?.Invoke(this, "Заявка отправлена");
                    return true;
                }
            }
            catch
            {
                // Переходим в оффлайн режим
                SetOffline();
            }
        }

        // Сохраняем локально
        var pending = new PendingCallback
        {
            Id = Guid.NewGuid().ToString(),
            Data = callback,
            CreatedAt = DateTime.UtcNow
        };
        
        _pendingCallbacks.Add(pending);
        SavePendingData();
        
        PendingCountChanged?.Invoke(this, _pendingCallbacks.Count);
        SyncStatusChanged?.Invoke(this, $"Сохранено локально ({_pendingCallbacks.Count} ожидает)");
        
        return true;
    }

    /// <summary>
    /// Попытка синхронизации ожидающих данных
    /// </summary>
    public async Task TrySyncPendingData()
    {
        if (!_isOnline || _pendingCallbacks.Count == 0) return;

        var synced = new List<PendingCallback>();
        
        foreach (var pending in _pendingCallbacks.ToArray())
        {
            try
            {
                var result = await _api.CreateCallbackAsync(pending.Data);
                if (result != null)
                {
                    synced.Add(pending);
                }
            }
            catch
            {
                SetOffline();
                break;
            }
        }

        if (synced.Count > 0)
        {
            foreach (var item in synced)
            {
                _pendingCallbacks.Remove(item);
            }
            
            SavePendingData();
            PendingCountChanged?.Invoke(this, _pendingCallbacks.Count);
            SyncStatusChanged?.Invoke(this, $"Синхронизировано {synced.Count} заявок");
        }
    }

    private async Task SendHeartbeat()
    {
        if (_sessionId == 0) return;
        
        try
        {
            var response = await _api.SendHeartbeatAsync(_sessionId);
            if (response != null)
            {
                SetOnline();
            }
        }
        catch
        {
            SetOffline();
        }
    }

    private async Task CheckConnection()
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var response = await client.GetAsync(_api.BaseUrl + "/health");
            
            if (response.IsSuccessStatusCode)
            {
                SetOnline();
                await TrySyncPendingData();
            }
            else
            {
                SetOffline();
            }
        }
        catch
        {
            SetOffline();
        }
    }

    private void SetOnline()
    {
        if (!_isOnline)
        {
            _isOnline = true;
            ConnectionStatusChanged?.Invoke(this, true);
            SyncStatusChanged?.Invoke(this, "Подключено");
        }
    }

    private void SetOffline()
    {
        if (_isOnline)
        {
            _isOnline = false;
            ConnectionStatusChanged?.Invoke(this, false);
            SyncStatusChanged?.Invoke(this, "Нет соединения");
        }
    }

    private void LoadPendingData()
    {
        try
        {
            if (File.Exists(_offlineDataPath))
            {
                var json = File.ReadAllText(_offlineDataPath);
                var data = JsonSerializer.Deserialize<List<PendingCallback>>(json);
                if (data != null)
                {
                    _pendingCallbacks.AddRange(data);
                }
            }
        }
        catch { }
    }

    private void SavePendingData()
    {
        try
        {
            var json = JsonSerializer.Serialize(_pendingCallbacks);
            File.WriteAllText(_offlineDataPath, json);
        }
        catch { }
    }

    public void Dispose()
    {
        Stop();
        _syncTimer.Dispose();
        _heartbeatTimer.Dispose();
        _connectionCheckTimer.Dispose();
    }
}

public class PendingCallback
{
    public string Id { get; set; } = string.Empty;
    public CallbackData Data { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}
