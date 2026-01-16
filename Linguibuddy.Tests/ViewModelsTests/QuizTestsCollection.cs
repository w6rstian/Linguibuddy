using FakeItEasy;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System;
using Xunit;

namespace Linguibuddy.Tests.ViewModelsTests;

[CollectionDefinition("QuizTests")]
public class QuizTestsCollection : ICollectionFixture<ApplicationFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}

public class ApplicationFixture : IDisposable
{
    public ApplicationFixture()
    {
        // Setup Application.Current globally for the collection
        var app = A.Fake<Application>();
        var resources = new ResourceDictionary
        {
            { "Primary", Colors.Blue },
            { "PrimaryDarkText", Colors.Black }
        };
        
        app.Resources = resources;
        Application.Current = app;
    }

    public void Dispose()
    {
        Application.Current = null;
    }
}
