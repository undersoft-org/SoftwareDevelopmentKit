﻿namespace System
{
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;

    public static class CultureHelper
    {
        public static void Use([DisallowNull] string culture, string uiCulture = null)
        {
            Use(new CultureInfo(culture), uiCulture == null ? null : new CultureInfo(uiCulture));
        }

        public static void Use([DisallowNull] CultureInfo culture, CultureInfo uiCulture = null)
        {
            var currentCulture = CultureInfo.CurrentCulture;
            var currentUiCulture = CultureInfo.CurrentUICulture;

            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = uiCulture ?? culture;
            CultureInfo.CurrentCulture = currentCulture;
            CultureInfo.CurrentUICulture = currentUiCulture;
        }

        public static bool IsRtl => CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft;

        public static bool IsValidCultureCode(string cultureCode)
        {
            if (string.IsNullOrEmpty(cultureCode))
            {
                return false;
            }

            try
            {
                _ = CultureInfo.GetCultureInfo(cultureCode);
                return true;
            }
            catch (CultureNotFoundException)
            {
                return false;
            }
        }

        public static string GetBaseCultureName(string cultureName)
        {
            return new CultureInfo(cultureName).Parent.Name;
        }

        public static bool IsCompatibleCulture(string sourceCultureName, string targetCultureName)
        {
            if (sourceCultureName == targetCultureName)
            {
                return true;
            }

            if (sourceCultureName.StartsWith("zh") && targetCultureName.StartsWith("zh"))
            {
                var culture = new CultureInfo(targetCultureName);
                do
                {
                    if (culture.Name == sourceCultureName)
                    {
                        return true;
                    }

                    culture = new CultureInfo(culture.Name).Parent;
                } while (!culture.Equals(CultureInfo.InvariantCulture));
            }

            if (sourceCultureName.Contains("-"))
            {
                return false;
            }

            if (!targetCultureName.Contains("-"))
            {
                return false;
            }

            if (sourceCultureName == GetBaseCultureName(targetCultureName))
            {
                return true;
            }

            return false;
        }
    }
}
