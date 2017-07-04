//------------------------------------------------------------------------------
// <copyright file="cmdGenerateFromProject.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace EFToDTO
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class cmdGenerateFromProject
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("8b222b6c-c2bc-4421-bc37-643d71ee5c4e");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly Package package;

        /// <summary>
        /// Initializes a new instance of the <see cref="cmdGenerateFromProject"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private cmdGenerateFromProject(Package package)
        {
            if (package == null)
            {
                throw new ArgumentNullException("package");
            }

            this.package = package;

            var commandService = ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                var menuCommandID = new CommandID(CommandSet, CommandId);
                var menuItem = new MenuCommand(MenuItemCallback, menuCommandID);
                commandService.AddCommand(menuItem);
            }
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static cmdGenerateFromProject Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IServiceProvider ServiceProvider
        {
            get
            {
                return package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static void Initialize(Package package)
        {
            Instance = new cmdGenerateFromProject(package);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void MenuItemCallback(object sender, EventArgs e)
        {
            const string modelConvensionName = "Model";
            const string dtoConvensionName = "Dto";
            var modelProject = HelperClass.Projects().FirstOrDefault(p=>p.Name.Contains(modelConvensionName));
            var dtoProject = HelperClass.Projects().FirstOrDefault(p => p.Name.Contains(dtoConvensionName));            
            
            var modelItems = HelperClass.GetProjectItemsOnlyClasses(modelProject?.ProjectItems);

            var generatedDtoCatalog = new List<KeyValuePair<string, List<string>>>();

            foreach (var c in modelItems)
            {
                var eles = c.FileCodeModel;

                if (eles == null)
                    continue;

                var lastNamespaceName = string.Empty;

                foreach (var elements in eles.CodeElements)
                {
                    if (!(elements is CodeNamespace)) continue;

                    var namespaceDte = elements as CodeNamespace;
                    if (string.IsNullOrEmpty(lastNamespaceName))
                    {
                        lastNamespaceName = namespaceDte.Name;
                    }

                    if (!lastNamespaceName.Equals(namespaceDte.Name))
                    {//es otro folder (verificar folder anterior)

                    }

                    // run through classes
                    foreach (var classElement in namespaceDte.Members)
                    {
                        var classDte = classElement as CodeClass;
                        if (classDte == null)
                            continue;
                        
                        var className = classDte.Name;
                        var parentName = string.Empty;

                        foreach (var currentClassBase in classDte.Bases)
                        {
                            var baseClassDte = currentClassBase as CodeClass;
                            parentName = baseClassDte?.Name?? typeof(Object).Name;
                        }

                        var optionalAbstract = classDte.IsAbstract ? "abstract " : "";
                        var optionalInheritance = !parentName.Equals(typeof(Object).Name)
                            ? " : " + parentName + dtoConvensionName
                            : "";

                        var baseDtoClassName = className + dtoConvensionName;

                        var classProperties = new List<string>
                        {
                            "using System;",
                            "",
                            "namespace " + namespaceDte.Name.Replace(modelConvensionName, dtoConvensionName),
                            "{",
                            "      public " + optionalAbstract + "class " + baseDtoClassName + optionalInheritance,
                            "      {"
                        };

                        foreach (var curMember in classDte.Members)
                        {
                            var curMemberProperty = curMember as CodeProperty;
                            var type = curMemberProperty?.Type.AsString;

                            if (curMemberProperty?.Access == vsCMAccess.vsCMAccessPublic)
                            {
                                var propertyForCheck = curMemberProperty as CodeProperty2;
                                if (propertyForCheck?.OverrideKind != vsCMOverrideKind.vsCMOverrideKindVirtual)
                                {
                                    //base class doesn't have Id (insert dto doesn't expose Id).
                                    if (!curMemberProperty.Name.Equals("Id"))
                                    {
                                        classProperties.Add(
                                            "            public virtual " + type + " " + curMemberProperty.Name +
                                            " { get; set; }");
                                    }
                                }
                            }
                        }
                        classProperties.Add("      }");
                        classProperties.Add("}");

                        //dtoProject.CodeModel.AddClass(baseDtoClassName, dtoProject.Name, -1,
                        //    new object[] {"System.Object"}, null, vsCMAccess.vsCMAccessPublic);
                        //dtoProject.CodeModel.AddVariable("id", dtoProject.Name + baseDtoClassName, "int", null,
                        //    vsCMAccess.vsCMAccessPublic);

                        generatedDtoCatalog.Add(new KeyValuePair<string, List<string>>(baseDtoClassName, classProperties));

                        if (!classDte.IsAbstract)
                        {
                            var insertDtoClassName = className + "Insert" + dtoConvensionName;

                            classProperties = new List<string>
                            {
                                "using System;",
                                "",
                                "namespace " + namespaceDte.Name.Replace(modelConvensionName, dtoConvensionName),
                                "{",
                                "      public class " + insertDtoClassName + " : " + baseDtoClassName,
                                "      {",
                                "      }",
                                "}"
                            };

                            generatedDtoCatalog.Add(
                                new KeyValuePair<string, List<string>>(insertDtoClassName, classProperties));

                            var getDtoClassName = className + "Get" + dtoConvensionName;

                            classProperties = new List<string>
                            {
                                "using System;",
                                "",
                                "namespace " + namespaceDte.Name.Replace(modelConvensionName, dtoConvensionName),
                                "{",
                                "      public class " + getDtoClassName + " : " + insertDtoClassName,
                                "      {",
                                "            public virtual int Id { get; set; }",
                                "      }",
                                "}"
                            };

                            generatedDtoCatalog.Add(
                                new KeyValuePair<string, List<string>>(getDtoClassName, classProperties));

                            var updateDtoClassName = className + "Update" + dtoConvensionName;

                            classProperties = new List<string>
                            {
                                "using System;",
                                "",
                                "namespace " + namespaceDte.Name.Replace(modelConvensionName, dtoConvensionName),
                                "{",
                                "      public class " + updateDtoClassName + " : " + insertDtoClassName,
                                "      {",
                                "            public virtual int Id { get; set; }",
                                "      }",
                                "}"
                            };

                            generatedDtoCatalog.Add(
                                new KeyValuePair<string, List<string>>(updateDtoClassName, classProperties));
                        }
                    }
                    
                }
            }

            var askFolder = new FolderBrowserDialog();
            if (askFolder.ShowDialog() == DialogResult.OK)
            {
                var filePath = askFolder.SelectedPath;
                foreach (var currentClass in generatedDtoCatalog)
                {
                    File.WriteAllLines(filePath +"\\"+ currentClass.Key + ".cs", currentClass.Value);
                }
            }

            MessageBox.Show("Dtos generated for " + dtoProject?.Name);
        }
    }
}
