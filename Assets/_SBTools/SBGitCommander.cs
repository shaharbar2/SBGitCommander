using System;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Shahar.Bar.Utils
{
    public class SBGitCommander : EditorWindow
    {
        private static Dictionary<string, string> _gitCommands = new();
        private static string _selectedCommandKey;
        private int _selectedCommandIndex = -1;
        private string _commandOutput = string.Empty;
        private Vector2 _scrollPosition;

        private static readonly string _fullPath = Path.Combine(Application.dataPath, "..", "SBTools", "GitCommands.txt");

        private static string _newCommand;
        private static string _newCommandNickname;

        [MenuItem("SBTools/Git Commander")]
        private static void Init()
        {
            var window = (SBGitCommander)GetWindow(typeof(SBGitCommander));
            window.titleContent = new GUIContent("Git Commander");
            
            OnScriptsReloaded();
            window.Show();
        }
        
        [InitializeOnLoadMethod]
        private static void OnScriptsReloaded()
        {
            EnsureDirectoryAndFileExist();
            LoadCommands();
            
            _selectedCommandKey = _gitCommands.Keys.FirstOrDefault();
            RefreshInfo();
        }
        
        private static void EnsureDirectoryAndFileExist()
        {
            var directoryInfo = new DirectoryInfo(_fullPath).Parent;

            if (directoryInfo == null) return;
            Directory.CreateDirectory(directoryInfo.FullName);

            if (File.Exists(_fullPath)) return;

            File.Create(_fullPath).Dispose();
            AddDefaultCommands();
        }


        private static void AddDefaultCommands()
        {
            AddCommand("Pull", "git pull");
            AddCommand("Push", "git push");
            AddCommand("Status", "git status");
            AddCommand("Add All", "git add .");
            AddCommand("Commit", "git commit -m \"\"");
        }

        private void OnGUI()
        {
            try
            {
                DisplayDropDownCommands();

                CommandInfo();

                Buttons();

                DisplayOutput();
            }
            catch (Exception ex)
            {
                EditorGUILayout.HelpBox("An error occurred: " + ex.Message, MessageType.Error);
            }

            TrySaveCommands();
        }

        private void InvokeButton()
        {
            GUIStyle invokeStyle = new(GUI.skin.button)
            {
                normal =
                {
                    textColor = Color.green
                },
                fontStyle = FontStyle.Bold,
            };

            if (GUILayout.Button("Invoke", invokeStyle, GUILayout.Width(100)))
            {
                ExecuteCommand(_gitCommands[_selectedCommandKey]);
            }
        }

        private void Buttons()
        {
            GUILayout.BeginHorizontal();
            AddEditCommands();
            DeleteButton();
            GUILayout.EndHorizontal();
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
            var outputStyle = new GUIStyle(GUI.skin.window)
            {
                richText = true,
                normal = { background = Texture2D.blackTexture, textColor = Color.green },
                hover = { background = Texture2D.blackTexture, textColor = Color.green },
                focused = { background = Texture2D.blackTexture, textColor = Color.green },
                active = { background = Texture2D.blackTexture, textColor = Color.green },
                stretchHeight = true,
                wordWrap = true,
                alignment = TextAnchor.UpperLeft
            };
            
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(300));
            GUILayout.TextArea(_commandOutput, outputStyle, GUILayout.ExpandHeight(true));
            GUILayout.EndScrollView();
        }

        private void DeleteButton()
        {
            GUIStyle deleteStyle = new(GUI.skin.button)
            {
                normal =
                {
                    textColor = Color.red
                }
            };

            if (_gitCommands.Count > 0 && GUILayout.Button("Delete", deleteStyle, GUILayout.Width(100)))
            {
                _gitCommands.Remove(_selectedCommandKey);
                _selectedCommandKey = _gitCommands.Keys.FirstOrDefault();
            }
        }

        private void DisplayDropDownCommands()
        {
            if (_gitCommands.Count <= 0) return;

            GUILayout.BeginHorizontal();

            var prevIndex = _selectedCommandIndex;
            _selectedCommandIndex = EditorGUILayout.Popup(_selectedCommandIndex, _gitCommands.Keys.ToArray());
            _selectedCommandKey = _gitCommands.Keys.ElementAtOrDefault(_selectedCommandIndex);
            InvokeButton();
            GUILayout.EndHorizontal();

            if (prevIndex == _selectedCommandIndex) return;

            RefreshInfo();
        }

        private static void RefreshInfo()
        {
            _newCommand = _gitCommands[_selectedCommandKey];
            _newCommandNickname = _selectedCommandKey;
        }

        private void AddEditCommands()
        {
            if (GUILayout.Button("New Command"))
            {
                AddCommand(_newCommandNickname, _newCommand);
            }

            if (_gitCommands.Count > 0 && GUILayout.Button("Override Chosen"))
            {
                _gitCommands[_selectedCommandKey] = _newCommand;
            }
        }

        private void CommandInfo()
        {
            var infoStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(10, 10, 10, 10),
                margin = new RectOffset(5, 5, 5, 5),
            };

            EditorGUILayout.BeginVertical(infoStyle);

            _newCommandNickname = EditorGUILayout.TextField("Nickname", _newCommandNickname);
            _newCommand = EditorGUILayout.TextField("Command", _newCommand);

            EditorGUILayout.EndVertical();
        }


        private static void AddCommand(string nickname, string command)
        {
            if (!string.IsNullOrWhiteSpace(command) && !string.IsNullOrWhiteSpace(nickname) && !_gitCommands.ContainsKey(nickname))
            {
                _gitCommands.Add(nickname, command);
                SaveCommands();
            }
        }

        private static void SaveCommands()
        {
            var folderPath = Path.Combine(Application.dataPath, "SBTools");
            Directory.CreateDirectory(folderPath);

            var lines = _gitCommands.Select(kvp => $"{kvp.Key}|{kvp.Value}");
            File.WriteAllLines(_fullPath, lines);
        }

        private static void LoadCommands()
        {
            if (!File.Exists(_fullPath)) return;

            var lines = File.ReadAllLines(_fullPath);
            _gitCommands = lines.ToDictionary(
                line => line.Split('|')[0],
                line => line.Split('|').Length > 1 ? line.Split('|')[1] : "");
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