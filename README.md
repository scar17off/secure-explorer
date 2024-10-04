# Secure Explorer

Secure Explorer is a Windows Forms application that provides a user-friendly interface for browsing files and directories while offering encryption and decryption capabilities for enhanced security.

## Features

- File and directory browsing
- File encryption and decryption using AES encryption
- Multi-threaded processing for improved performance
- Context menu for easy encryption/decryption operations
- Progress bar to track encryption/decryption progress

## How to Use

1. Launch the application.
2. Use the text box at the bottom to navigate to different directories.
3. Double-click on folders to open them or files to view details.
4. Enter a password in the designated text box for encryption/decryption operations.
5. Right-click on files or folders to access the context menu for encryption/decryption options.
6. Use the "Update" button to refresh the file list.
7. Adjust the number of threads for processing in the rightmost text box.

## Security Features

- AES encryption for secure file protection
- Password-based key derivation using PBKDF2 with 50,000 iterations
- Unique salt generation for each encryption operation

## Note

Always remember your encryption password, as it is required for decryption. There is no way to recover encrypted files without the correct password.

## Requirements

- .NET Framework (version compatible with the project)
- Windows operating system

## Disclaimer

This tool is for educational and personal use only. Always backup your important files before using encryption software.

# License

This project is licensed under the MIT License. See the LICENSE file for more details.