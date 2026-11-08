using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Data;
using Avalonia.Markup.Xaml;

namespace Grimoire.Services;

public sealed record LanguageOption(string? Code, string NativeName)
{
    /// <summary>Native language name; the "system default" entry is shown in the current language.</summary>
    public string DisplayName => Code is null ? Localizer.Instance["Language_System"] : NativeName;

    public override string ToString() => DisplayName;
}

/// <summary>
/// Runtime-switchable string table. Views use <c>{l:Tr Key}</c>, which produces an observable binding
/// that updates instantly when <see cref="SetLanguage"/> is called. No reflection, so it is trim/AOT safe.
/// </summary>
public sealed class Localizer
{
    public const string FallbackLanguage = "en";
    public static Localizer Instance { get; } = new();

    private readonly Dictionary<string, Subject> _subjects = new();
    private IReadOnlyDictionary<string, string> _table = Strings.Tables[FallbackLanguage];

    private Localizer() => SetLanguage(null);

    public string LanguageCode { get; private set; } = FallbackLanguage;

    public event EventHandler? LanguageChanged;

    public static IReadOnlyList<LanguageOption> Languages { get; } =
        [new(null, ""), .. Strings.Tables.Keys.Select(code => new LanguageOption(code, Strings.Tables[code]["LanguageName"]))];

    public string this[string key] =>
        _table.TryGetValue(key, out var value) || Strings.Tables[FallbackLanguage].TryGetValue(key, out value) ? value : key;

    public string Format(string key, params object[] args) => string.Format(CultureInfo.CurrentUICulture, this[key], args);

    /// <param name="code">Two-letter language code, or null to follow the operating system.</param>
    public void SetLanguage(string? code)
    {
        code ??= CultureInfo.InstalledUICulture.TwoLetterISOLanguageName;
        if (!Strings.Tables.ContainsKey(code)) code = FallbackLanguage;

        LanguageCode = code;
        _table = Strings.Tables[code];
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(code);
        }
        catch (CultureNotFoundException) { } // invariant-globalization builds (e.g. WASM) may lack cultures

        foreach (var (key, subject) in _subjects)
            subject.OnNext(this[key]);
        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }

    public IObservable<string> Observe(string key)
    {
        if (!_subjects.TryGetValue(key, out var subject))
            _subjects[key] = subject = new Subject(this[key]);
        return subject;
    }

    /// <summary>Minimal behaviour-subject: replays the current value to new subscribers.</summary>
    private sealed class Subject(string initial) : IObservable<string>
    {
        private readonly List<IObserver<string>> _observers = [];
        private string _value = initial;

        public void OnNext(string value)
        {
            _value = value;
            foreach (var o in _observers.ToArray()) o.OnNext(value);
        }

        public IDisposable Subscribe(IObserver<string> observer)
        {
            _observers.Add(observer);
            observer.OnNext(_value);
            return new Unsubscriber(() => _observers.Remove(observer));
        }

        private sealed class Unsubscriber(Action dispose) : IDisposable
        {
            public void Dispose() => dispose();
        }
    }
}

/// <summary>XAML: <c>Text="{l:Tr Library_Title}"</c>.</summary>
public sealed class TrExtension : MarkupExtension
{
    public TrExtension() { }
    public TrExtension(string key) => Key = key;

    public string Key { get; set; } = "";

    public override object ProvideValue(IServiceProvider serviceProvider) =>
        Localizer.Instance.Observe(Key).ToBinding();
}
