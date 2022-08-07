/*
This file is part of Shabby.

Shabby is free software: you can redistribute it and/or
modify it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

Shabby is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with Shabby.  If not, see
<http://www.gnu.org/licenses/>.
*/

using System.Reflection;

// KSP assembly information

// The assembly version has the format "{Major}.{Minor}.{Build}.{Revision}".
// The form "{Major}.{Minor}.*" will automatically update the build and revision,
// and "{Major}.{Minor}.{Build}.*" will update just the revision.
[assembly: AssemblyVersionAttribute("{{ ver_major }}.{{ ver_minor }}.{{ ver_patch }}")]
[assembly: AssemblyInformationalVersionAttribute("{{ ver_major }}.{{ ver_minor }}.{{ ver_patch }}")]
[assembly: KSPAssembly("Shabby", {{ ver_major }}, {{ ver_minor }}, {{ ver_patch }})]

// Information about this assembly is defined by the following attributes. 
// Change them to the values specific to your project.
[assembly: AssemblyCopyright("2022 Bill Currie")]