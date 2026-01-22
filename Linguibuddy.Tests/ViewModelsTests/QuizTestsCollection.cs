using FakeItEasy;

namespace Linguibuddy.Tests.ViewModelsTests;

[CollectionDefinition("QuizTests")]
public class QuizTestsCollection : ICollectionFixture<ApplicationFixture>
{
}

public class ApplicationFixture : IDisposable
{
    public ApplicationFixture()
    {
        var app = A.Fake<Application>();
        var resources = new ResourceDictionary
        {
            { "Primary", Colors.Blue },
            { "PrimaryDark", Colors.DarkBlue },
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