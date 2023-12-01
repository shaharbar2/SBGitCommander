using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;

namespace Shahar.Bar.Utils
{
    public class SBPackageMaker : EditorWindow
    {
        private string _sourceFolderPath;
        private string _packageFolderPath;
        private string _packageName;
        private string _packageVersion = "1.0.0";
        private string _packageDisplayName;
        private string _packageDescription;
        private string _licenseEntity;

        [MenuItem("SBTools/UPM Package Creator")]
        private static void Init()
        {
            var window = (SBPackageMaker)GetWindow(typeof(SBPackageMaker));
            window.titleContent = new GUIContent("UPM Package Creator");
            window.Show();
        }

        private void OnGUI()
        {
            GUILayout.Label("Source Folder", EditorStyles.boldLabel);

            if (GUILayout.Button("Select Source Folder", GUILayout.Width(200)))
            {
                _sourceFolderPath = EditorUtility.OpenFolderPanel("Select Source Folder", "", "");
            }

            if (!string.IsNullOrEmpty(_sourceFolderPath))
            {
                EditorGUILayout.TextField("Selected Path:", _sourceFolderPath);
            }

            GUILayout.Label("Package Settings", EditorStyles.boldLabel);
            _packageName = TextFieldWithPlaceholder("Name:", _packageName, "com.example.mypackage");
            _packageDisplayName = TextFieldWithPlaceholder("Display Name:", _packageDisplayName, "My Package");
            _packageDescription = TextFieldWithPlaceholder("Description:", _packageDescription, "Description of what the package does.");
            _packageVersion = TextFieldWithPlaceholder("Version:", _packageVersion, "1.0.0");
            _licenseEntity = TextFieldWithPlaceholder("License Entity:", _licenseEntity, "Shahar Bar (SBTools)"); 
            
            if (GUILayout.Button("Create Package"))
            {
                CreateUPMPackage();
            }
        }


        private string TextFieldWithPlaceholder(string label, string value, string placeholder)
        {
            EditorGUI.BeginChangeCheck();
            var newValue = EditorGUILayout.TextField(label, string.IsNullOrEmpty(value) ? placeholder : value);

            if (EditorGUI.EndChangeCheck())
            {
                return newValue != placeholder ? newValue : "";
            }

            return value;
        }

        private void CreateUPMPackage()
        {
            _packageFolderPath = Path.Combine(Application.dataPath, "..", "Packages", _packageName);

            if (!Directory.Exists(_sourceFolderPath))
            {
                Debug.LogError("Source folder does not exist.");
                return;
            }

            if (Directory.Exists(_packageFolderPath))
            {
                return;
            }

            Directory.CreateDirectory(_packageFolderPath);

            var editorScripts = new List<(string, string)>();
            var testScripts = new List<(string, string)>();
            var runtimeScripts = new List<(string, string)>();

            foreach (var filePath in Directory.GetFiles(_sourceFolderPath, "*.cs", SearchOption.AllDirectories))
            {
                var fileContent = File.ReadAllText(filePath);

                var relativePath = filePath.Substring(_sourceFolderPath.Length + 1);
                var targetPath = Path.Combine(_packageFolderPath, relativePath);

                if (fileContent.Contains("Editor"))
                {
                    editorScripts.Add((targetPath, filePath));
                }
                else if (fileContent.Contains("Tests"))
                {
                    testScripts.Add((targetPath, filePath));
                }
                else
                {
                    runtimeScripts.Add((targetPath, filePath));
                }
            }

            CopyFilesToSubfolder(editorScripts, "Editor");
            CopyFilesToSubfolder(testScripts, "Tests");
            CopyFilesToSubfolder(runtimeScripts, "Runtime");

            string packageJson = GeneratePackageJson();
            File.WriteAllText(Path.Combine(_packageFolderPath, "package.json"), packageJson);

            CreateLicenseFile();
            CreateReadmeFile();

            AssetDatabase.Refresh();

            Debug.Log("Package created successfully.");
        }

        private void CopyFilesToSubfolder(List<(string, string)> files, string subfolderName)
        {
            if (files.Count == 0) return;

            var subfolderPath = Path.Combine(_packageFolderPath, subfolderName);
            Directory.CreateDirectory(subfolderPath);

            // Call to create the asmdef for the subfolder
            CreateSubfolderAndAsmdef(subfolderName, _packageName);

            foreach (var file in files)
            {
                var destinationFilePath = Path.Combine(subfolderPath, Path.GetFileName(file.Item1));
                File.Copy(file.Item2, destinationFilePath, true);
            }
        }

        private void CreateSubfolderAndAsmdef(string folderName, string packageName)
        {
            var folderPath = Path.Combine(_packageFolderPath, folderName);
            Directory.CreateDirectory(folderPath);

            var asmdefContent = GenerateAsmdefContent(packageName, folderName, folderName == "Editor");
            File.WriteAllText(Path.Combine(folderPath, $"{packageName}.{folderName}.asmdef"), asmdefContent);
        }

        private string GenerateAsmdefContent(string packageName, string folderName, bool onlyEditor)
        {
            var includeEditor = onlyEditor ? @"""Editor""" : "";
            return $@"{{
  ""name"": ""{packageName}.{folderName}"",
  ""references"": [],
  ""includePlatforms"": [{includeEditor}],
  ""excludePlatforms"": [],
  ""allowUnsafeCode"": false,
  ""overrideReferences"": false,
  ""precompiledReferences"": [],
  ""autoReferenced"": true,
  ""defineConstraints"": [],
  ""versionDefines"": []
}}";
        }

        private string GeneratePackageJson() =>
            $@"{{
  ""name"": ""{_packageName}"",
  ""version"": ""{_packageVersion}"",
  ""displayName"": ""{_packageDisplayName}"",
  ""description"": ""{_packageDescription}"",
  ""unity"": ""2020.1"",
  ""dependencies"": {{}}
}}";

        private static void CopyFiles(string sourcePath, string destinationPath)
        {
            foreach (var dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
                Directory.CreateDirectory(dirPath.Replace(sourcePath, destinationPath));

            foreach (var newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
                File.Copy(newPath, newPath.Replace(sourcePath, destinationPath), true);
        }

        private void CreateLicenseFile()
        {
            string licenseText = $@"MIT License

Copyright (c) {DateTime.Now.Year} Shahar Bar {_licenseEntity}

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the ""Software""), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.";

            File.WriteAllText(Path.Combine(_packageFolderPath, "LICENSE.md"), licenseText);
        }

        private void CreateReadmeFile()
        {
            string readmeText = $@"# {_packageDisplayName}

{_packageDescription}

## Installation

To install this package, add the following line to the `dependencies` section of your project's `manifest.json` file:
""{_packageName}"": ""https://github.com/shaharbar2/SBTools.git?path=/Packages/{_packageName}#main""";
                
                File.WriteAllText(Path.Combine(_packageFolderPath, "README.md"), readmeText);
        }
    }
}
