using System;

namespace DK.Implementation.Virtual
{
    class VirtualFileSystemException : Exception
    {
        public VirtualFileSystemException(string message) : base(message) { }
    }

    class InvalidVirtualPathException : VirtualFileSystemException
    {
        public InvalidVirtualPathException(string path) : base($"Invalid path '{path}'.") { }
    }

    class VirtualFileNotFoundException : VirtualFileSystemException
    {
        public VirtualFileNotFoundException(string pathName) : base($"File '{pathName}' could not be found.") { }
    }

    class VirtualDirectoryNotFoundException : VirtualFileSystemException
    {
        public VirtualDirectoryNotFoundException(string path) : base($"Directory '{path}' could not be found.") { }
    }

    class InvalidVirtualFileNameException : VirtualFileSystemException
    {
        public InvalidVirtualFileNameException(string fileName) : base($"Invalid file name '{fileName}'.") { }
    }

    class InvalidVirtualDirectoryNameException : VirtualFileSystemException
    {
        public InvalidVirtualDirectoryNameException(string fileName) : base($"Invalid directory name '{fileName}'.") { }
    }
}
