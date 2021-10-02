using System;

namespace Gibbed.Cryptic.FileFormats
{
    public static class InvariantShorthand
    {
        public static string _(FormattableString formattable)
        {
            return FormattableString.Invariant(formattable);
        }
    }
}
