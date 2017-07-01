using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;

namespace EFToDTO
{
    using System.Collections.Generic;
    using EnvDTE;
    using EnvDTE80;

    public static class HelperClass
    {
        public static DTE2 GetActiveIDE()
        {
            // Get an instance of currently running Visual Studio IDE.
            var dte2 = Package.GetGlobalService(typeof(DTE)) as DTE2;
            return dte2;
        }

        public static IList<Project> Projects()
        {
            var projects = GetActiveIDE().Solution.Projects;
            var list = new List<Project>();
            var item = projects.GetEnumerator();
            while (item.MoveNext())
            {
                var project = item.Current as Project;
                if (project == null)
                {
                    continue;
                }

                if (project.Kind == ProjectKinds.vsProjectKindSolutionFolder)
                {
                    list.AddRange(GetSolutionFolderProjects(project));
                }
                else
                {
                    list.Add(project);
                }
            }

            return list;
        }

        private static IEnumerable<Project> GetSolutionFolderProjects(Project solutionFolder)
        {
            var list = new List<Project>();
            for (var i = 1; i <= solutionFolder.ProjectItems.Count; i++)
            {
                var subProject = solutionFolder.ProjectItems.Item(i).SubProject;
                if (subProject == null)
                {
                    continue;
                }

                // If this is another solution folder, do a recursive call, otherwise add
                if (subProject.Kind == ProjectKinds.vsProjectKindSolutionFolder)
                {
                    list.AddRange(GetSolutionFolderProjects(subProject));
                }
                else
                {
                    list.Add(subProject);
                }
            }
            return list;
        }

        /// <summary>
        /// Recursively gets all the ProjectItem objects in a list of projectitems from a Project
        /// </summary>
        /// <param name="projectItems">The project items.</param>
        /// <returns></returns>
        public static IEnumerable<ProjectItem> GetProjectItems(ProjectItems projectItems)
        {
            foreach (ProjectItem item in projectItems)
            {
                yield return item;

                if (item.SubProject != null)
                {
                    foreach (ProjectItem childItem in GetProjectItems(item.SubProject.ProjectItems))
                        yield return childItem;
                }
                else
                {
                    foreach (ProjectItem childItem in GetProjectItems(item.ProjectItems))
                        yield return childItem;
                }
            }

        }

        /// <summary>
        /// Recursively gets all the ProjectItem objects in a list of projectitems from a Project
        /// </summary>
        /// <param name="projectItems">The project items.</param>
        /// <returns></returns>
        public static IEnumerable<ProjectItem> GetProjectItemsOnlyClasses(ProjectItems projectItems)
            => GetProjectItems(projectItems).Where(v => v.Name.Contains(".cs"));

    }

}
