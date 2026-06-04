using System;
using TripleDetection.Infrastructure.Persistence;

namespace TripleDetection.Presentation
{

public static class DatabaseConfig
{
    public static void Initialize()
    {
        DatabaseInitializer.Initialize();
    }
}
}