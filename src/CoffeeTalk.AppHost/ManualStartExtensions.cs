using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace CoffeeTalk.AppHost;

public static class ManualStartExtensions
{
    public static IResourceBuilder<TResource> AsManualStart<TResource>(
        this IResourceBuilder<TResource> builder)
        where TResource : class, IResource
    {
        return builder.WithAnnotation(new ExplicitStartupAnnotation());
    }
}
