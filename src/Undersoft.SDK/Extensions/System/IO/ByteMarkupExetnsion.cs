
// <copyright file="ByteMarkupExetnsion.cs" company="UltimatR.Core">
//     Copyright (c) Undersoft. All rights reserved.
// </copyright>



/// <summary>
/// The IO namespace.
/// </summary>
namespace System.IO
{



    /// <summary>
    /// Class ByteMarkupExtension.
    /// </summary>
    public static class ByteMarkupExtension
    {
        #region Methods







        /// <summary>
        /// Determines whether the specified noisekind is markup.
        /// </summary>
        /// <param name="checknoise">The checknoise.</param>
        /// <param name="noisekind">The noisekind.</param>
        /// <returns><c>true</c> if the specified noisekind is markup; otherwise, <c>false</c>.</returns>
        public static bool IsMarkup(this byte checknoise, out MarkupKind noisekind)
        {
            switch (checknoise)
            {
                case (byte)MarkupKind.Block:
                    noisekind = MarkupKind.Block;
                    return true;
                case (byte)MarkupKind.End:
                    noisekind = MarkupKind.End;
                    return true;
                case (byte)MarkupKind.Empty:
                    noisekind = MarkupKind.Empty;
                    return false;
                default:
                    noisekind = MarkupKind.None;
                    return false;
            }
        }







        /// <summary>
        /// Determines whether the specified spliterkind is spliter.
        /// </summary>
        /// <param name="checknoise">The checknoise.</param>
        /// <param name="spliterkind">The spliterkind.</param>
        /// <returns><c>true</c> if the specified spliterkind is spliter; otherwise, <c>false</c>.</returns>
        public static bool IsSpliter(this byte checknoise, out MarkupKind spliterkind)
        {
            switch (checknoise)
            {
                case (byte)MarkupKind.Empty:
                    spliterkind = MarkupKind.Empty;
                    return true;
                case (byte)MarkupKind.Line:
                    spliterkind = MarkupKind.Line;
                    return true;
                case (byte)MarkupKind.Space:
                    spliterkind = MarkupKind.Space;
                    return true;
                case (byte)MarkupKind.Semi:
                    spliterkind = MarkupKind.Semi;
                    return true;
                case (byte)MarkupKind.Coma:
                    spliterkind = MarkupKind.Coma;
                    return true;
                case (byte)MarkupKind.Colon:
                    spliterkind = MarkupKind.Colon;
                    return true;
                case (byte)MarkupKind.Dot:
                    spliterkind = MarkupKind.Dot;
                    return true;
                default:
                    spliterkind = MarkupKind.None;
                    return false;
            }
        }

        #endregion
    }
}
