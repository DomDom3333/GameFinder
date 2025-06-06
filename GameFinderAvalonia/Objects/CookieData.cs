using System;
using OpenQA.Selenium;

namespace GameFinderAvalonia.Objects;

public class CookieData
{
    public string Name { get; set; }
    public string Value { get; set; }
    public string Domain { get; set; }
    public string Path { get; set; }
    public DateTime? Expiry { get; set; }
    public bool Secure { get; set; }
    public bool HttpOnly { get; set; }
    public string SameSite { get; set; }

    public CookieData()
    {
    }

    public CookieData(Cookie cookie)
    {
        Name = cookie.Name;
        Value = cookie.Value;
        Domain = cookie.Domain;
        Path = cookie.Path;
        Expiry = cookie.Expiry;
        Secure = cookie.Secure;
        HttpOnly = cookie.IsHttpOnly;
        SameSite = cookie.SameSite;
    }

    public Cookie ToSeleniumCookie()
    {
        return new Cookie(Name, Value, Domain, Path, Expiry, Secure, HttpOnly, SameSite);
    }
}
