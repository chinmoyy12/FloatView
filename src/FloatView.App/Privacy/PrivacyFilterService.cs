using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using FloatView.App.Models;
using FloatView.App.Persistence;

namespace FloatView.App.Privacy;

public class PrivacyFilterService
{
    private readonly SettingsService _settingsService;
    private readonly ConcurrentDictionary<string, PrivacyRule> _rules = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _allowlist = new(StringComparer.OrdinalIgnoreCase);
    private long _totalBlockedCount = 0;

    public event Action<long>? TotalBlockedCountChanged;

    public PrivacyFilterService(SettingsService settingsService)
    {
        _settingsService = settingsService;
        Initialize();
    }

    public bool IsEnabled
    {
        get => _settingsService.Settings.PrivacyShieldEnabled;
        set
        {
            _settingsService.Settings.PrivacyShieldEnabled = value;
            _settingsService.SaveSettings(_settingsService.Settings);
        }
    }

    public long TotalBlockedCount => _totalBlockedCount;

    public IReadOnlyCollection<PrivacyRule> Rules => _rules.Values.ToList();
    public IReadOnlyCollection<string> Allowlist => _allowlist.ToList();

    public void Initialize()
    {
        _rules.Clear();
        var savedRules = _settingsService.LoadPrivacyRules();
        foreach (var rule in savedRules)
        {
            _rules[rule.Domain] = rule;
            _totalBlockedCount += rule.BlockCount;
        }

        _allowlist.Clear();
        foreach (var host in _settingsService.Settings.PrivacyAllowlist)
        {
            _allowlist.Add(host);
        }
    }

    public bool ShouldBlockUri(Uri? requestUri, string? topLevelHost, out string matchedDomain)
    {
        matchedDomain = string.Empty;
        if (!IsEnabled || requestUri == null) return false;

        // If top-level website is explicitly in user allowlist, don't filter
        if (!string.IsNullOrEmpty(topLevelHost) && _allowlist.Contains(topLevelHost))
        {
            return false;
        }

        var requestHost = requestUri.Host;
        if (string.IsNullOrEmpty(requestHost)) return false;

        // Check against allowlist
        if (_allowlist.Contains(requestHost)) return false;

        // Never block common essential endpoints
        if (requestHost.EndsWith("login.live.com", StringComparison.OrdinalIgnoreCase) ||
            requestHost.EndsWith("accounts.google.com", StringComparison.OrdinalIgnoreCase) ||
            requestHost.EndsWith("appleid.apple.com", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // Match against known tracker / ad domains
        foreach (var kvp in _rules)
        {
            var domain = kvp.Key;
            if (requestHost.Equals(domain, StringComparison.OrdinalIgnoreCase) ||
                requestHost.EndsWith("." + domain, StringComparison.OrdinalIgnoreCase))
            {
                if (kvp.Value.IsBlocked)
                {
                    matchedDomain = domain;
                    kvp.Value.BlockCount++;
                    kvp.Value.LastBlockedAt = DateTime.UtcNow;
                    _totalBlockedCount++;
                    TotalBlockedCountChanged?.Invoke(_totalBlockedCount);
                    return true;
                }
            }
        }

        return false;
    }

    public void AddRule(string domain, bool isBlocked = true)
    {
        domain = domain.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(domain)) return;

        var rule = new PrivacyRule
        {
            Domain = domain,
            IsBlocked = isBlocked,
            BlockCount = 0,
            LastBlockedAt = DateTime.UtcNow
        };
        _rules[domain] = rule;
        SaveRules();
    }

    public void RemoveRule(string domain)
    {
        if (_rules.TryRemove(domain, out _))
        {
            SaveRules();
        }
    }

    public void ToggleRule(string domain)
    {
        if (_rules.TryGetValue(domain, out var rule))
        {
            rule.IsBlocked = !rule.IsBlocked;
            SaveRules();
        }
    }

    public void AddToAllowlist(string host)
    {
        host = host.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(host)) return;

        _allowlist.Add(host);
        _settingsService.Settings.PrivacyAllowlist = _allowlist.ToList();
        _settingsService.SaveSettings(_settingsService.Settings);
    }

    public void RemoveFromAllowlist(string host)
    {
        host = host.Trim().ToLowerInvariant();
        _allowlist.Remove(host);
        _settingsService.Settings.PrivacyAllowlist = _allowlist.ToList();
        _settingsService.SaveSettings(_settingsService.Settings);
    }

    public void ResetToDefaults()
    {
        _rules.Clear();
        var defaults = SettingsService.GetDefaultPrivacyRules();
        foreach (var d in defaults)
        {
            _rules[d.Domain] = d;
        }
        SaveRules();
    }

    private void SaveRules()
    {
        _settingsService.SavePrivacyRules(_rules.Values.ToList());
    }
}
