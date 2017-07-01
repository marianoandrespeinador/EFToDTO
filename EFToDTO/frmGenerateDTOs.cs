//------------------------------------------------------------------------------
// <copyright file="frmGenerateDTOs.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace EFToDTO
{
    using System;
    using System.Runtime.InteropServices;
    using Microsoft.VisualStudio.Shell;

    /// <summary>
    /// This class implements the tool window exposed by this package and hosts a user control.
    /// </summary>
    /// <remarks>
    /// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane,
    /// usually implemented by the package implementer.
    /// <para>
    /// This class derives from the ToolWindowPane class provided from the MPF in order to use its
    /// implementation of the IVsUIElementPane interface.
    /// </para>
    /// </remarks>
    [Guid("3e2301ab-4aa2-4706-9c5c-8ae841b68bd3")]
    public class frmGenerateDTOs : ToolWindowPane
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="frmGenerateDTOs"/> class.
        /// </summary>
        public frmGenerateDTOs() : base(null)
        {
            Caption = "frmGenerateDTOs";

            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
            // the object returned by the Content property.
            Content = new frmGenerateDTOsControl();
        }
    }
}
