﻿// nConfigure detects dependecies between your .net project files
// Copyright (C) 2008,2009  Magnus Berglund, nConfigure@gmail.com
            
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.

// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;
using System.Text.RegularExpressions;

namespace nConfigureLib
{
	/// <summary>
	/// Creates a msbuild file with all dependencies.
	/// </summary>
    class MsbuildBuilder
    {
        public void CreateBuildFile(List<CsProject> projects, XmlWriter writer)
        {
            writer.WriteStartDocument();
            writer.WriteComment("This file is generated by nConfigure." + GPLText.Create());
            
            writer.WriteStartElement("Project", "http://schemas.microsoft.com/developer/msbuild/2003");
            writer.WriteAttributeString("DefaultTargets", "build");

            writer.WriteStartElement("PropertyGroup");
            writer.WriteAttributeString("Condition", "'$(Configuration)'==''");
            writer.WriteElementString("Configuration", "Debug");
            writer.WriteEndElement();//</PropertyGroup>

            CreateItemGroup(writer, projects);

            foreach (CsProject project in projects)
            {
                CreateProjectTarget(writer, project);
            }
            writer.WriteEndElement();//Project
            writer.WriteEndDocument();
            writer.Flush();

        }

        private static void CreateItemGroup(XmlWriter w, List<CsProject> projects)
        {

            bool first = true;

            foreach (CsProject project in projects)
            {
                if (first)
                {
                    w.WriteStartElement("ItemGroup");
                    first = false;
                }
                w.WriteStartElement("Targets");
                w.WriteAttributeString("Include", DeMsbuildString(project.FullAbsoluteFileName));
                w.WriteElementString("ProjectName", project.ProjectName);
                w.WriteElementString("ProjectType", project.ProjectType.ToString());
                w.WriteElementString("CsProjPath", project.FullAbsoluteFileName);
                w.WriteElementString("GUID", project.Guid.ToString());
                w.WriteEndElement();
            }
            if (!first)
            {
                w.WriteEndElement();
            }
        }

        private static void CreateProjectTarget(XmlWriter w, CsProject project)
        {
            w.WriteStartElement("Target");
            w.WriteAttributeString("Name", DeMsbuildString(project.FullAbsoluteFileName));
            StringBuilder dependsOn = new StringBuilder();
            List<Reference>.Enumerator enumerator = project.References.GetEnumerator();

            bool first = true;
            while (enumerator.MoveNext())
            {
                if (enumerator.Current.CsProject != null)
                {
                    //this is a reference to a project that we have found the .csproj file to
                    //so we could include it in the build script

                    if (first)
                        first = false;
                    else
                        dependsOn.Append(";");

                    dependsOn.Append(DeMsbuildString(enumerator.Current.CsProject.FullAbsoluteFileName));
                }
            }
            if (dependsOn.Length > 0)
                w.WriteAttributeString("DependsOnTargets", dependsOn.ToString());

            w.WriteStartElement("MSBuild");
            w.WriteAttributeString("Projects", project.FullAbsoluteFileName);
            //w.WriteAttributeString("StopOnFirstFailure","true" );
            w.WriteAttributeString("Targets", "build");
            w.WriteAttributeString("Properties", "Configuration=$(Configuration);Platform=$(Platform)");
            w.WriteEndElement(); //</MsBuild>
            w.WriteEndElement(); //</Target>
            w.Flush();
        }

        /// <summary>
        /// msbuild seems to dislike '(', ')' and  '.' so we replace them with '_'
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        private static string DeMsbuildString(string s)
        {
            string tmp = s.Replace('(', '_');
            tmp = tmp.Replace(')', '_');
            tmp = tmp.Replace('\\', '_');
            tmp = tmp.Replace('/', '_');
            return tmp.Replace('.', '_');
        }
    }
}