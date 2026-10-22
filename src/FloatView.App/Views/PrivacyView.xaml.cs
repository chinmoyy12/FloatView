using System.Windows;
using System.Windows.Controls;
using FloatView.App.Models;
using FloatView.App.Privacy;

namespace FloatView.App.Views;

public partial class PrivacyView : UserControl
{
    private PrivacyFilterService? _privacyService;

    public PrivacyView()
    {
        InitializeComponent();
    }

    public void Initialize(PrivacyFilterService privacyService)
    {
        _privacyService = privacyService;
        _privacyService.TotalBlockedCountChanged += OnTotalBlockedCountChanged;

        UpdateShieldButton();
        RefreshLists();
        TxtTotalBlocked.Text = _privacyService.TotalBlockedCount.ToString();
    }

    private void OnTotalBlockedCountChanged(long count)
    {
        Dispatcher.Invoke(() => TxtTotalBlocked.Text = count.ToString());
    }

    private void UpdateShieldButton()
    {
        if (_privacyService == null) return;
        bool active = _privacyService.IsEnabled;
        BtnToggleShield.Content = active ? "Shield Active" : "Shield Disabled";
        BtnToggleShield.Background = active
            ? (System.Windows.Media.Brush)FindResource("BrushAccentGradient")
            : (System.Windows.Media.Brush)FindResource("BrushBgHover");
    }

    private void RefreshLists()
    {
        if (_privacyService == null) return;
        LstAllowlist.ItemsSource = _privacyService.Allowlist;
        LstRules.ItemsSource = _privacyService.Rules;
    }

    private void OnToggleShieldClicked(object sender, RoutedEventArgs e)
    {
        if (_privacyService == null) return;
        _privacyService.IsEnabled = !_privacyService.IsEnabled;
        UpdateShieldButton();
    }

    private void OnAddAllowlistClicked(object sender, RoutedEventArgs e)
    {
        if (_privacyService == null) return;
        var host = TxtNewAllowlistHost.Text.Trim();
        if (!string.IsNullOrWhiteSpace(host))
        {
            _privacyService.AddToAllowlist(host);
            TxtNewAllowlistHost.Text = string.Empty;
            RefreshLists();
        }
    }

    private void OnRemoveAllowlistClicked(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string host && _privacyService != null)
        {
            _privacyService.RemoveFromAllowlist(host);
            RefreshLists();
        }
    }

    private void OnAddRuleClicked(object sender, RoutedEventArgs e)
    {
        if (_privacyService == null) return;
        var domain = TxtNewRuleDomain.Text.Trim();
        if (!string.IsNullOrWhiteSpace(domain))
        {
            _privacyService.AddRule(domain, true);
            TxtNewRuleDomain.Text = string.Empty;
            RefreshLists();
        }
    }

    private void OnToggleRuleClicked(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is PrivacyRule rule && _privacyService != null)
        {
            _privacyService.ToggleRule(rule.Domain);
            RefreshLists();
        }
    }

    private void OnDeleteRuleClicked(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is PrivacyRule rule && _privacyService != null)
        {
            _privacyService.RemoveRule(rule.Domain);
            RefreshLists();
        }
    }

    private void OnResetRulesClicked(object sender, RoutedEventArgs e)
    {
        _privacyService?.ResetToDefaults();
        RefreshLists();
    }
}
