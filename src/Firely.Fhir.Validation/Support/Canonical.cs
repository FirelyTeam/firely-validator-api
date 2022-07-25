/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.Rest;
using System;
using System.Runtime.Serialization;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// Represents a FHIR canonical uri, which is an uri plus an optional version indicator.
    /// </summary>
    [DataContract]
    public record Canonical
    {
        /// <summary>
        /// Returns a canonical for the given FHIR core datatype or resource
        /// </summary>
        public static Canonical ForCoreType(string coreType) =>
            new(ResourceIdentity.CORE_BASE_URL + coreType);

        /// <summary>
        /// The unparsed original string, as passed to the constructor.
        /// </summary>
        [DataMember]
        public string Original { get; private set; }

        /// <summary>
        /// Constructs a canonical from a string.
        /// </summary>
        /// <param name="original"></param>
        public Canonical(string original)
        {
            Original = original ?? throw new ArgumentNullException(nameof(original));

            (Uri, Version, Anchor) = splitCanonical(original);
        }

        /// <summary>
        /// Deconstructs the canonical into its uri and version.
        /// </summary>
        public void Deconstruct(out string? uri, out string? version, out string? anchor)
        {
            uri = Uri;
            version = Version;
            anchor = Anchor;
        }

        /// <summary>
        /// The uri part of the canonical, which is the canonical without the version indication.
        /// </summary>
        public string? Uri { get; private set; }

        /// <summary>
        /// The version string of the canonical (if present).
        /// </summary>
        public string? Version { get; private set; }

        /// <summary>
        /// Optional anchor at the end of the canonical.
        /// </summary>
        public string? Anchor { get; private set; }

        /// <summary>
        /// Whether the canonical is a relative or an absolute uri.
        /// </summary>
        public bool IsAbsolute => ToUri().IsAbsoluteUri;

        /// <summary>
        /// Whether the canonical has a version part.
        /// </summary>
        public bool HasVersion => Version is not null;

        /// <summary>
        /// Whether the canonical end with an anchor.
        /// </summary>
        public bool HasAnchor => Anchor is not null;

        /// <summary>
        /// Converts the canonical back to the full canonical as passed to the
        /// constructor.
        /// </summary>
        /// <remarks>Returns the <see cref="Original" /> property.</remarks>
        public override string ToString() => Original;

        /// <summary>
        /// Converts the canonical to a <see cref="System.Uri" />.
        /// </summary>
        /// <returns></returns>
        public Uri ToUri() => new(Original, UriKind.RelativeOrAbsolute);

        /// <summary>
        /// Converts a string to a canonical.
        /// </summary>
        public static implicit operator Canonical(string s) => new(s);

        /// <summary>
        /// Converts a canonical to a string.
        /// </summary>
        /// <remarks>Returns the <see cref="Original" /> property.</remarks>
        public static explicit operator string(Canonical c) => c.Original;

        /// <summary>
        /// Splits a canonical into its url, version and anchor string.
        /// </summary>
        private static (string? url, string? version, string? anchor) splitCanonical(string canonical)
        {
            var (rest, a) = splitOff(canonical, '#');
            var (u, v) = splitOff(rest, '|');

            return (u == String.Empty ? null : u, v, a);

            static (string, string?) splitOff(string url, char separator)
            {
                if (url.EndsWith(separator)) url = url[..^1];
                var position = url.LastIndexOf(separator);

                return position == -1 ?
                    (url, null)
                    : (url[0..position], url[(position + 1)..]);
            }
        }

    }
}
