#!/usr/bin/env python3
"""
Validation script to verify the Visual Studio extension project structure
and core interfaces are properly set up for Task 1.
"""

import os
import re
from pathlib import Path

def check_file_exists(file_path, description):
    """Check if a file exists and report the result."""
    if os.path.exists(file_path):
        print(f"✅ {description}: {file_path}")
        return True
    else:
        print(f"❌ {description}: {file_path} - NOT FOUND")
        return False

def check_interface_definition(file_path, interface_name):
    """Check if an interface is properly defined in a file."""
    if not os.path.exists(file_path):
        print(f"❌ Interface {interface_name}: File {file_path} not found")
        return False
    
    with open(file_path, 'r') as f:
        content = f.read()
        
    # Check for interface declaration
    interface_pattern = rf'public\s+interface\s+{interface_name}'
    if re.search(interface_pattern, content):
        print(f"✅ Interface {interface_name}: Properly defined")
        return True
    else:
        print(f"❌ Interface {interface_name}: Not properly defined")
        return False

def check_csproj_references(csproj_path):
    """Check if the csproj file has all required references."""
    if not os.path.exists(csproj_path):
        print(f"❌ Project file not found: {csproj_path}")
        return False
    
    with open(csproj_path, 'r') as f:
        content = f.read()
    
    required_packages = [
        'Microsoft.VisualStudio.SDK',
        'Microsoft.VSSDK.BuildTools',
        'Microsoft.VisualStudio.Shell.Framework',
        'LibGit2Sharp',
        'Newtonsoft.Json'
    ]
    
    missing_packages = []
    for package in required_packages:
        if package not in content:
            missing_packages.append(package)
    
    if not missing_packages:
        print("✅ All required NuGet packages are referenced")
        return True
    else:
        print(f"❌ Missing NuGet packages: {', '.join(missing_packages)}")
        return False

def check_vsix_manifest(manifest_path):
    """Check if the VSIX manifest is properly configured."""
    if not os.path.exists(manifest_path):
        print(f"❌ VSIX manifest not found: {manifest_path}")
        return False
    
    with open(manifest_path, 'r') as f:
        content = f.read()
    
    required_elements = [
        'DisplayName',
        'Description',
        'Identity',
        'Installation',
        'Assets'
    ]
    
    missing_elements = []
    for element in required_elements:
        if f'<{element}' not in content:
            missing_elements.append(element)
    
    if not missing_elements:
        print("✅ VSIX manifest is properly configured")
        return True
    else:
        print(f"❌ VSIX manifest missing elements: {', '.join(missing_elements)}")
        return False

def main():
    """Main validation function."""
    print("🔍 Validating Visual Studio Extension Project Structure")
    print("=" * 60)
    
    base_path = "AiderVSExtension"
    all_checks_passed = True
    
    # Check core project files
    core_files = [
        (f"{base_path}/AiderVSExtension.csproj", "Project file"),
        (f"{base_path}/AiderVSExtensionPackage.cs", "Main package class"),
        (f"{base_path}/source.extension.vsixmanifest", "VSIX manifest"),
        (f"{base_path}/Properties/AssemblyInfo.cs", "Assembly info"),
        (f"{base_path}/Services/ServiceContainer.cs", "Service container")
    ]
    
    print("\n📁 Core Project Files:")
    for file_path, description in core_files:
        if not check_file_exists(file_path, description):
            all_checks_passed = False
    
    # Check interface files
    interfaces = [
        ("IAiderService", "Core Aider AI service interface"),
        ("IConfigurationService", "Configuration management interface"),
        ("IFileContextService", "File and solution context interface"),
        ("IAIModelManager", "AI model management interface"),
        ("ICompletionProvider", "AI completion provider interface"),
        ("IErrorHandler", "Error handling interface"),
        ("IMessageRenderer", "Message rendering interface")
    ]
    
    print("\n🔌 Interface Definitions:")
    for interface_name, description in interfaces:
        file_path = f"{base_path}/Interfaces/{interface_name}.cs"
        if not check_interface_definition(file_path, interface_name):
            all_checks_passed = False
    
    # Check project configuration
    print("\n⚙️ Project Configuration:")
    if not check_csproj_references(f"{base_path}/AiderVSExtension.csproj"):
        all_checks_passed = False
    
    if not check_vsix_manifest(f"{base_path}/source.extension.vsixmanifest"):
        all_checks_passed = False
    
    # Check service container integration
    print("\n🏗️ Service Container Integration:")
    package_file = f"{base_path}/AiderVSExtensionPackage.cs"
    if os.path.exists(package_file):
        with open(package_file, 'r') as f:
            content = f.read()
        
        if 'ServiceContainer' in content and 'InitializeAsync' in content:
            print("✅ Service container is properly integrated in main package")
        else:
            print("❌ Service container integration incomplete")
            all_checks_passed = False
    
    # Final result
    print("\n" + "=" * 60)
    if all_checks_passed:
        print("🎉 All validation checks PASSED!")
        print("✅ Task 1 requirements have been successfully implemented:")
        print("   • VSIX project with proper manifest and package configuration")
        print("   • Core interfaces defined for services and components")
        print("   • Dependency injection container set up for service management")
    else:
        print("❌ Some validation checks FAILED!")
        print("Please review the issues above and fix them.")
    
    return all_checks_passed

if __name__ == "__main__":
    success = main()
    exit(0 if success else 1)