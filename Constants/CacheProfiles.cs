using System;
using Microsoft.AspNetCore.Mvc;

namespace ApiEcommerce.Constants;

public class CacheProfiles
{
    public const string Default30 = "Default30";
    public const string Default10 = "Default10";
    public static readonly CacheProfile Profile10 = new() { Duration = 10 };
    public static readonly CacheProfile Profile30 = new() { Duration = 30 };
}
