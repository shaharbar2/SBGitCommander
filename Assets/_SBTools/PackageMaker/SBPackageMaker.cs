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

        private string _lastCheckedPackageName = "";

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

            EditorGUI.BeginChangeCheck();
            _packageName = EditorGUILayout.TextField("Name:", _packageName);
            if (EditorGUI.EndChangeCheck())
            {
                // Check if the package name has changed and is not empty
                if (!string.IsNullOrEmpty(_packageName) && _packageName != _lastCheckedPackageName)
                {
                    LoadPackageDataIfExists(_packageName);
                    _lastCheckedPackageName = _packageName;
                }
            }
            
            _packageDisplayName = TextFieldWithPlaceholder("Display Name:", _packageDisplayName, "My Package");
            _packageDescription = TextFieldWithPlaceholder("Description:", _packageDescription, "Description of what the package does.");
            _packageVersion = TextFieldWithPlaceholder("Version:", _packageVersion, "1.0.0");
            _licenseEntity = TextFieldWithPlaceholder("License Entity:", _licenseEntity, "Shahar Bar (SBTools)");

            if (GUILayout.Button("Create Package"))
            {
                CreateUPMPackage();
            }
        }

        private void LoadPackageDataIfExists(string packageName)
        {
            var packagePath = Path.Combine(Application.dataPath, "..", "Packages", packageName);
            var packageJsonPath = Path.Combine(packagePath, "package.json");

            if (!File.Exists(packageJsonPath)) return;
            
            var packageJson = File.ReadAllText(packageJsonPath);
            ParsePackageJson(packageJson);
        }

        private void ParsePackageJson(string json)
        {
            PackageJson packageData = JsonUtility.FromJson<PackageJson>(json);
    
            if (packageData != null)
            {
                _packageName = packageData.name;
                _packageVersion = packageData.version;
                _packageDisplayName = packageData.displayName;
                _packageDescription = packageData.description;

                // Update any other fields if necessary
            }
            else
            {
                Debug.LogError("Failed to parse package.json");
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

            Directory.CreateDirectory(_packageFolderPath);

            var editorScripts = new List<(string, string)>();
            var testScripts = new List<(string, string)>();
            var runtimeScripts = new List<(string, string)>();

            foreach (var filePath in Directory.GetFiles(_sourceFolderPath, "*.*", SearchOption.AllDirectories))
            {
                var fileContent = File.ReadAllText(filePath);

                var relativePath = filePath[(_sourceFolderPath.Length + 1)..];
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

            if (ShouldGenerateFile("package.json"))
            {
                var packageJson = GeneratePackageJson();
                File.WriteAllText(Path.Combine(_packageFolderPath, "package.json"), packageJson);
            }

            if (ShouldGenerateFile("LICENSE.md"))
            {
                CreateLicenseFile();
            }

            if (ShouldGenerateFile("README.md"))
            {
                CreateReadmeFile();
            }

            AssetDatabase.Refresh();
            Debug.Log("Package created successfully.");
        }

        private bool ShouldGenerateFile(string fileName)
        {
            var filePath = Path.Combine(_packageFolderPath, fileName);
            return !File.Exists(filePath) || EditorUtility.DisplayDialog("File Exists", $"The file {fileName} already exists. Do you want to overwrite it?", "Yes", "No");
        }

        private void CopyFilesToSubfolder(List<(string, string)> files, string subfolderName)
        {
            if (files.Count == 0) return;

            var subfolderPath = Path.Combine(_packageFolderPath, subfolderName);
            Directory.CreateDirectory(subfolderPath);

            CreateSubfolderAndAsmdef(subfolderName, _packageName);

            foreach (var file in files)
            {
                var destinationFilePath = Path.Combine(subfolderPath, Path.GetFileName(file.Item1));
                if (File.Exists(destinationFilePath))
                {
                    if (!EditorUtility.DisplayDialog("File Exists", $"The file {Path.GetFileName(destinationFilePath)} already exists. Do you want to overwrite it?", "Yes", "No"))
                    {
                        continue;
                    }
                }

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

        private void CreateLicenseFile()
        {
            var licenseText = $@"MIT License

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
            var readmeText = $@"# {_packageDisplayName}

{_packageDescription}

## Installation

To install this package, add the following line to the `dependencies` section of your project's `manifest.json` file:
""{_packageName}"": ""https://github.com/shaharbar2/SBTools.git?path=/Packages/{_packageName}#main""";

            File.WriteAllText(Path.Combine(_packageFolderPath, "README.md"), readmeText);
        }
    }
    
    [Serializable]
    public class PackageJson
    {
        public string name;
        public string version;
        public string displayName;
        public string description;
    }
}