# SB Git Commander

Tool for invoking git commands from editor
![img.png](img.png)
![img_1.png](img_1.png)
## Installation

To install this package, add the following line to the `dependencies` section of your project's `manifest.json` file:
"com.sb.git-commander": "https://github.com/shaharbar2/SBTools.git?path=/Packages/com.sb.git-commander#main"

# SB Git Commander

`SBGitCommander` is a Unity Editor tool for executing Git commands within the Unity Editor.
This tool is part of the `Shahar.Bar.Utils` namespace. and is designed to facilitate Git operations directly from the 
Unity interface, enhancing the workflow for Unity developers who use Git for version control.

## Features

- **User Interface:** Provides a GUI within the Unity Editor to manage and execute Git commands.
- **Command Management:** Enables users to add, edit, and delete custom Git commands.
- **Command Execution:** Allows for the execution of Git commands and displays the output within the Unity Editor.
- **Custom Command Storage:** Saves custom Git commands in a file for persistent access across sessions.
- **Predefined Commands:** Includes default Git commands like Pull, Push, Status, Add All, and Commit.

## How to Use

1. **Open S BGitCommander:** In Unity, navigate to `SBTools > Git Commander` to open the tool.
2. **Manage Commands:** Add, edit, or delete Git commands using the provided interface.
3. **Execute Commands:** Select a command from the dropdown list and click 'Invoke' to execute it. The output will be displayed in the tool's window.

## Requirements

- Unity Editor (version 2020.1 or higher).
- Git must be installed and accessible from the command line.

## Contributions

For improvements or bug reports, please reach out to the maintainer at `https://github.com/shaharbar2/SBTools`.
Feel free to fork and create Pull Requests.

## License

`SBGitCommander` is distributed under the MIT License.
For more details about the license, see the included [LICENSE.md](LICENSE.md) file.