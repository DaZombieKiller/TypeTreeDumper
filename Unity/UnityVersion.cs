using System;
using System.Text.RegularExpressions;

namespace Unity
{
    public enum UnityVersionType
    {
        Alpha,
        Beta,
        Final,
        Patch,
        Experimental
    }

    public readonly struct UnityVersion : IComparable<UnityVersion>, IEquatable<UnityVersion>
    {
        public readonly int Major;
        
        public readonly int Minor;

        public readonly int Patch;
        
        public readonly UnityVersionType Type;

        public readonly int Build;

        public static readonly UnityVersion Unity3_4 = new UnityVersion(3, 4);

        public static readonly UnityVersion Unity3_5 = new UnityVersion(3, 5);

        public static readonly UnityVersion Unity4_0 = new UnityVersion(4, 0);

        public static readonly UnityVersion Unity4_1 = new UnityVersion(4, 1);

        public static readonly UnityVersion Unity4_2 = new UnityVersion(4, 2);

        public static readonly UnityVersion Unity4_3 = new UnityVersion(4, 3);

        public static readonly UnityVersion Unity4_4 = new UnityVersion(4, 4);

        public static readonly UnityVersion Unity4_5 = new UnityVersion(4, 5);

        public static readonly UnityVersion Unity4_6 = new UnityVersion(4, 6);

        public static readonly UnityVersion Unity4_7 = new UnityVersion(4, 7);

        public static readonly UnityVersion Unity5_0 = new UnityVersion(5, 0);

        public static readonly UnityVersion Unity5_1 = new UnityVersion(5, 1);

        public static readonly UnityVersion Unity5_2 = new UnityVersion(5, 2);

        public static readonly UnityVersion Unity5_3 = new UnityVersion(5, 3);

        public static readonly UnityVersion Unity5_4 = new UnityVersion(5, 4);

        public static readonly UnityVersion Unity5_5 = new UnityVersion(5, 5);

        public static readonly UnityVersion Unity5_6 = new UnityVersion(5, 6);

        public static readonly UnityVersion Unity2017_1 = new UnityVersion(2017, 1);

        public static readonly UnityVersion Unity2017_2 = new UnityVersion(2017, 2);

        public static readonly UnityVersion Unity2017_3 = new UnityVersion(2017, 3);

        public static readonly UnityVersion Unity2017_4 = new UnityVersion(2017, 4);

        public static readonly UnityVersion Unity2018_1 = new UnityVersion(2018, 1);

        public static readonly UnityVersion Unity2018_2 = new UnityVersion(2018, 2);

        public static readonly UnityVersion Unity2018_3 = new UnityVersion(2018, 3);

        public static readonly UnityVersion Unity2018_4 = new UnityVersion(2018, 4);

        public static readonly UnityVersion Unity2019_1 = new UnityVersion(2019, 1);

        public static readonly UnityVersion Unity2019_2 = new UnityVersion(2019, 2);

        public static readonly UnityVersion Unity2019_3 = new UnityVersion(2019, 3);

        public static readonly UnityVersion Unity2019_4 = new UnityVersion(2019, 4);

        public static readonly UnityVersion Unity2020_1 = new UnityVersion(2020, 1);

        public static readonly UnityVersion Unity2020_2 = new UnityVersion(2020, 2);

        public UnityVersion(int major, int minor)
            : this(major, minor, 0, 0, 0)
        {
        }

        public UnityVersion(int major, int minor, int patch)
            : this(major, minor, patch, 0, 0)
        {
        }

        public UnityVersion(int major, int minor, int patch, char type, int build)
            : this(major, minor, patch, VersionTypeFromChar(type), build)
        {
        }

        public UnityVersion(int major, int minor, int patch, UnityVersionType type, int build)
        {
            Major = major;
            Minor = minor;
            Patch = patch;
            Type  = type;
            Build = build;
        }

        public UnityVersion(string version)
        {
            var match = Regex.Match(version, @"(\d+)\.(\d+)\.(\d+)([abfpx])(\d+)");

            if (!match.Success)
                throw new ArgumentException("Invalid version string.", nameof(version));

            Major = int.Parse(match.Groups[1].Value);
            Minor = int.Parse(match.Groups[2].Value);
            Patch = int.Parse(match.Groups[3].Value);
            Type  = VersionTypeFromChar(match.Groups[4].Value[0]);
            Build = int.Parse(match.Groups[5].Value);
        }

        public int CompareTo(UnityVersion other)
        {
            if (Major != other.Major)
                return Major.CompareTo(other.Major);

            if (Minor != other.Minor)
                return Minor.CompareTo(other.Minor);

            if (Patch != other.Patch)
                return Patch.CompareTo(other.Patch);

            if (Type != other.Type)
                return Type.CompareTo(other.Type);

            return Build.CompareTo(other.Build);
        }

        public static bool operator ==(UnityVersion a, UnityVersion b) => a.CompareTo(b) == 0;

        public static bool operator !=(UnityVersion a, UnityVersion b) => a.CompareTo(b) != 0;

        public static bool operator >(UnityVersion a, UnityVersion b) => a.CompareTo(b) > 0;

        public static bool operator <(UnityVersion a, UnityVersion b) => a.CompareTo(b) < 0;

        public static bool operator >=(UnityVersion a, UnityVersion b) => a.CompareTo(b) >= 0;

        public static bool operator <=(UnityVersion a, UnityVersion b) => a.CompareTo(b) <= 0;

        public bool Equals(UnityVersion other) => CompareTo(other) == 0;

        public override bool Equals(object obj) => obj is UnityVersion version && Equals(version);

        public override int GetHashCode() => HashCode.Combine(Major, Minor, Patch, Type, Build);

        public override string ToString() => string.Format("{0}.{1}.{2}{3}{4}", Major, Minor, Patch, CharFromVersionType(Type), Build);

        static char CharFromVersionType(UnityVersionType type) => type switch
        {
            UnityVersionType.Alpha        => 'a',
            UnityVersionType.Beta         => 'b',
            UnityVersionType.Final        => 'f',
            UnityVersionType.Patch        => 'p',
            UnityVersionType.Experimental => 'x',
            _                             => throw new ArgumentOutOfRangeException(nameof(type))
        };

        static UnityVersionType VersionTypeFromChar(char c) => c switch
        {
            'a' => UnityVersionType.Alpha,
            'b' => UnityVersionType.Beta,
            'f' => UnityVersionType.Final,
            'p' => UnityVersionType.Patch,
            'x' => UnityVersionType.Experimental,
            _   => throw new ArgumentOutOfRangeException(nameof(c))
        };
    }
}
