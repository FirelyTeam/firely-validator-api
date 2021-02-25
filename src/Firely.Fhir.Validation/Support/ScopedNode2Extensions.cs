using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using System;
using System.Linq;

namespace Firely.Fhir.Validation
{
    internal static class ScopedNode2Extensions
    {
        /// <summary>
        /// Turn a relative reference into an absolute url, based on the fullUrl of the parent resource
        /// </summary>
        /// <param name="node"></param>
        /// <param name="identity"></param>
        /// <returns></returns>
        /// <remarks>See https://www.hl7.org/fhir/bundle.html#references for more information</remarks>
        public static ResourceIdentity MakeAbsolute(this ScopedNode2 node, ResourceIdentity identity)
        {
            if (identity.IsRelativeRestUrl)
            {
                // Relocate the relative url on the base given in the fullUrl of the entry (if applicable)
                var fullUrl = node.FullUrl();

                if (fullUrl != null)
                {
                    var parentIdentity = new ResourceIdentity(fullUrl);

                    if (parentIdentity.IsAbsoluteRestUrl)
                        identity = identity.WithBase(parentIdentity.BaseUri);
                    else if (parentIdentity.IsUrn)
                        identity = new ResourceIdentity($"{parentIdentity}/{identity.Id}");
                }

                // Return the identity - will remain relative if we did not find a fullUrl              
            }

            return identity;
        }

        /// <summary>
        /// Where this item is a reference, resolve it to an actual resource, and return that
        /// </summary>
        /// <param name="element"></param>
        /// <param name="externalResolver"></param>
        /// <returns></returns>
        public static T? Resolve<T>(this T element, Func<string, T>? externalResolver = null) where T : class, ITypedElement
        {
            // First, get the url to fetch from the focus
            string? url = null;

            if (element.InstanceType == "string" && element.Value is string s)
                url = s;
            else if (element.InstanceType == "Reference")
                url = element.ParseResourceReference().Reference;

            if (url == null) return default;   // nothing found to resolve

            return Resolve(element, url, externalResolver);
        }

        public static ResourceReference ParseResourceReference(this ITypedElement instance)
        {
            return new ResourceReference()
            {
                Reference = instance.Children("reference").GetString(),
                Display = instance.Children("display").GetString()
            };
        }


        public static T? Resolve<T>(this T element, string reference, Func<string, T>? externalResolver = null) where T : class, ITypedElement
        {
            // Then, resolve the url within the instance data first - this is only
            // possibly if we have a ScopedNavigator at hand
            if (element is ScopedNode2 scopedNode)
            {
                var identity = scopedNode.MakeAbsolute(new ResourceIdentity(reference));

                if (identity.IsLocal || identity.IsAbsoluteRestUrl || identity.IsUrn)
                {
                    var result = locateResource(identity);
                    if (result != null) return (T)(object)result;
                }
            }

            // Nothing found internally, now try the external resolver
            return externalResolver is not null ? externalResolver(reference) : default;

            ScopedNode2? locateResource(ResourceIdentity identity)
            {
                var url = identity.ToString();

                foreach (var parent in scopedNode.ParentResources())
                {
                    if (parent.InstanceType == "Bundle")
                    {
                        var result = parent.BundledResources().FirstOrDefault(br => br.FullUrl == url)?.Resource;
                        if (result != null) return result;
                    }
                    else
                    {
                        if (parent.Id() == url) return parent;
                        var result = parent.ContainedResources().FirstOrDefault(cr => cr.Id() == url);
                        if (result != null) return result;
                    }
                }

                return default;
            }
        }

        public static string? ParseReference(this ScopedNode2 node)
            => node.Children("reference").GetString();
    }
}
