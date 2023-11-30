using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Shahar.Bar.Utils
{
    public class SbGitCommander : EditorWindow
    {
        private List<string> _gitCommands = new();
        private int _selectedCommandIndex;
        private string _commandOutput = string.Empty;
        private Vector2 _scrollPosition;
        private string _filePath = "Assets/GitCommands.txt";

        private string _newCommand;

        [MenuItem("SBTools/Git Commander")]
        private static void Init()
        {
            var window = (SbGitCommander)GetWindow(typeof(SbGitCommander));
            window.titleContent = new GUIContent("Git Commander");
            window.LoadCommands();
            window.Show();
        }

        private void OnGUI()
        {
            AddEditComands();

            InvokeOrDeleteCommands();

            DisplayOutput();

            TrySaveCommands();
        }

        private void TrySaveCommands()
        {
            if (GUI.changed)
            {
                SaveCommands();
            }
        }

        private void DisplayOutput()
        {
            GUILayout.Label("Command Output", EditorStyles.boldLabel);
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(100));
            GUILayout.TextArea(_commandOutput, GUILayout.ExpandHeight(true));
            GUILayout.EndScrollView();
        }

        private void InvokeOrDeleteCommands()
        {
            GUILayout.Space(10);

            GUILayout.Label("Git Commands", EditorStyles.boldLabel);

            if (_gitCommands.Count > 0)
            {
                _selectedCommandIndex = EditorGUILayout.Popup("Commands", _selectedCommandIndex, _gitCommands.ToArray());
            }

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Invoke"))
            {
                ExecuteCommand(_gitCommands[_selectedCommandIndex]);
            }

            if (_gitCommands.Count > 0 && GUILayout.Button("Remove"))
            {
                _gitCommands.RemoveAt(_selectedCommandIndex);
                _selectedCommandIndex = 0;
            }

            GUILayout.EndHorizontal();
        }

        private void AddEditComands()
        {
            GUILayout.Label("Add New Command", EditorStyles.boldLabel);
            _newCommand = EditorGUILayout.TextField("Command", _newCommand);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Command"))
            {
                AddCommand(_newCommand);
            }

            if (_gitCommands.Count > 0 && GUILayout.Button("Edit"))
            {
                _gitCommands[_selectedCommandIndex] = _newCommand;
            }

            GUILayout.EndHorizontal();
        }

        private void AddCommand(string command)
        {
            if (!string.IsNullOrWhiteSpace(command) && !_gitCommands.Contains(command))
            {
                _gitCommands.Add(command);
                SaveCommands();
            }
        }

        private void SaveCommands()
        {
            File.WriteAllLines(_filePath, _gitCommands);
        }

        private void LoadCommands()
        {
            _filePath = Path.Combine(Application.dataPath, "..", "SBTools");

            if (File.Exists(_filePath))
            {
                _gitCommands = File.ReadAllLines(_filePath).ToList();
            }
        }

        private void ExecuteCommand(string command)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = command.Replace("git", ""),
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            _commandOutput = $"Output:\n{output}\nError:\n{error}";
        }
    }
}