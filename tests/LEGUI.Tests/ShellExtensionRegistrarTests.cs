using Microsoft.Win32;
using NSubstitute;
using Xunit;

namespace LEGUI.Tests;

public class ShellExtensionRegistrarTests
{
    private const string TestClsid = "{12345678-ABCD-EF01-2345-6789ABCDEF01}";
    private const string OldClsid = ShellExtensionRegistrar.OldClsid;
    private const string TestDllPath = @"C:\Program Files\LE\ShellExtension.dll";
    private const string FriendlyName = "Locale Emulator Shell Extension";

    private readonly IRegistryOperations _mockRegistry;
    private readonly Dictionary<string, List<(string? name, object value)>> _writtenKeys;

    public ShellExtensionRegistrarTests()
    {
        _mockRegistry = Substitute.For<IRegistryOperations>();
        _writtenKeys = new Dictionary<string, List<(string? name, object value)>>();

        _mockRegistry.CreateSubKey(
            Arg.Any<RegistryHive>(),
            Arg.Any<string>(),
            Arg.Any<RegistryView>())
            .Returns(callInfo =>
            {
                var subKey = callInfo.ArgAt<string>(1);
                var values = new List<(string? name, object value)>();
                _writtenKeys[subKey] = values;

                var mockKey = Substitute.For<IRegistryKeyWrapper>();
                mockKey.SetValue(Arg.Any<string?>(), Arg.Any<object>());
                mockKey.When(k => k.SetValue(Arg.Any<string?>(), Arg.Any<object>()))
                    .Do(ci =>
                    {
                        values.Add((ci.ArgAt<string?>(0), ci.ArgAt<object>(1)));
                    });

                return mockKey;
            });
    }

    [Fact]
    public void Register_AllUsers_WritesToHKLM()
    {
        var registrar = new ShellExtensionRegistrar(_mockRegistry, TestClsid);

        registrar.Register(
            ShellExtensionRegistrar.InstallMode.AllUsers,
            TestDllPath);

        _mockRegistry.Received().CreateSubKey(
            RegistryHive.LocalMachine,
            Arg.Any<string>(),
            Arg.Any<RegistryView>());

        _mockRegistry.DidNotReceive().CreateSubKey(
            RegistryHive.CurrentUser,
            Arg.Any<string>(),
            Arg.Any<RegistryView>());
    }

    [Fact]
    public void Register_CurrentUser_WritesToHKCU()
    {
        var registrar = new ShellExtensionRegistrar(_mockRegistry, TestClsid);

        registrar.Register(
            ShellExtensionRegistrar.InstallMode.CurrentUser,
            TestDllPath);

        _mockRegistry.Received().CreateSubKey(
            RegistryHive.CurrentUser,
            Arg.Any<string>(),
            Arg.Any<RegistryView>());

        _mockRegistry.DidNotReceive().CreateSubKey(
            RegistryHive.LocalMachine,
            Arg.Any<string>(),
            Arg.Any<RegistryView>());
    }

    [Fact]
    public void Register_WritesInprocServer32()
    {
        var registrar = new ShellExtensionRegistrar(_mockRegistry, TestClsid);

        registrar.Register(
            ShellExtensionRegistrar.InstallMode.AllUsers,
            TestDllPath);

        var expectedKey = $@"Software\Classes\CLSID\{TestClsid}\InprocServer32";
        Assert.True(_writtenKeys.ContainsKey(expectedKey),
            $"Expected key '{expectedKey}' not written. Written keys: {string.Join(", ", _writtenKeys.Keys)}");

        var values = _writtenKeys[expectedKey];
        // (Default) value = DLL path, ThreadingModel = Apartment
        Assert.Contains(values, v => v.name == null && (string)v.value == TestDllPath);
        Assert.Contains(values, v => v.name == "ThreadingModel" && (string)v.value == "Apartment");
    }

    [Fact]
    public void Register_WritesContextMenuHandler()
    {
        var registrar = new ShellExtensionRegistrar(_mockRegistry, TestClsid);

        registrar.Register(
            ShellExtensionRegistrar.InstallMode.AllUsers,
            TestDllPath);

        var expectedKey = $@"Software\Classes\*\shellex\ContextMenuHandlers\{TestClsid}";
        Assert.True(_writtenKeys.ContainsKey(expectedKey),
            $"Expected key '{expectedKey}' not written. Written keys: {string.Join(", ", _writtenKeys.Keys)}");

        var values = _writtenKeys[expectedKey];
        Assert.Contains(values, v => v.name == null && (string)v.value == FriendlyName);
    }

    [Fact]
    public void Register_On64BitOS_UsesRegistry64View()
    {
        var registrar = new ShellExtensionRegistrar(_mockRegistry, TestClsid);

        registrar.Register(
            ShellExtensionRegistrar.InstallMode.AllUsers,
            TestDllPath,
            is64BitOs: true);

        _mockRegistry.Received().CreateSubKey(
            Arg.Any<RegistryHive>(),
            Arg.Any<string>(),
            RegistryView.Registry64);
    }

    [Fact]
    public void Register_On32BitOS_UsesDefaultView()
    {
        var registrar = new ShellExtensionRegistrar(_mockRegistry, TestClsid);

        registrar.Register(
            ShellExtensionRegistrar.InstallMode.AllUsers,
            TestDllPath,
            is64BitOs: false);

        _mockRegistry.Received().CreateSubKey(
            Arg.Any<RegistryHive>(),
            Arg.Any<string>(),
            RegistryView.Default);
    }

    [Fact]
    public void Unregister_AllUsers_DeletesFromHKLM()
    {
        var registrar = new ShellExtensionRegistrar(_mockRegistry, TestClsid);

        registrar.Unregister(ShellExtensionRegistrar.InstallMode.AllUsers);

        _mockRegistry.Received().DeleteSubKeyTree(
            RegistryHive.LocalMachine,
            $@"Software\Classes\CLSID\{TestClsid}",
            Arg.Any<RegistryView>(),
            false);

        _mockRegistry.Received().DeleteSubKeyTree(
            RegistryHive.LocalMachine,
            $@"Software\Classes\*\shellex\ContextMenuHandlers\{TestClsid}",
            Arg.Any<RegistryView>(),
            false);
    }

    [Fact]
    public void Unregister_CurrentUser_DeletesFromHKCU()
    {
        var registrar = new ShellExtensionRegistrar(_mockRegistry, TestClsid);

        registrar.Unregister(ShellExtensionRegistrar.InstallMode.CurrentUser);

        _mockRegistry.Received().DeleteSubKeyTree(
            RegistryHive.CurrentUser,
            $@"Software\Classes\CLSID\{TestClsid}",
            Arg.Any<RegistryView>(),
            false);

        _mockRegistry.Received().DeleteSubKeyTree(
            RegistryHive.CurrentUser,
            $@"Software\Classes\*\shellex\ContextMenuHandlers\{TestClsid}",
            Arg.Any<RegistryView>(),
            false);
    }

    [Fact]
    public void IsInstalled_Delegates_ToSubKeyExists()
    {
        _mockRegistry.SubKeyExists(
            RegistryHive.LocalMachine,
            Arg.Any<string>(),
            Arg.Any<RegistryView>())
            .Returns(true);

        var registrar = new ShellExtensionRegistrar(_mockRegistry, TestClsid);

        var result = registrar.IsInstalled(ShellExtensionRegistrar.InstallMode.AllUsers);
        Assert.True(result);
    }

    [Fact]
    public void HasOldRegistration_ChecksBothHives()
    {
        _mockRegistry.SubKeyExists(
            Arg.Any<RegistryHive>(),
            Arg.Is<string>(s => s.Contains(OldClsid)),
            Arg.Any<RegistryView>())
            .Returns(true);

        var registrar = new ShellExtensionRegistrar(_mockRegistry, TestClsid);

        var result = registrar.HasOldRegistration();
        Assert.True(result);
    }

    [Fact]
    public void CleanupOldRegistration_DeletesOldClsidFromBothHives()
    {
        var registrar = new ShellExtensionRegistrar(_mockRegistry, TestClsid);

        registrar.CleanupOldRegistration();

        // Should clean HKLM
        _mockRegistry.Received().DeleteSubKeyTree(
            RegistryHive.LocalMachine,
            Arg.Is<string>(s => s.Contains(OldClsid)),
            Arg.Any<RegistryView>(),
            false);

        // Should clean HKCU
        _mockRegistry.Received().DeleteSubKeyTree(
            RegistryHive.CurrentUser,
            Arg.Is<string>(s => s.Contains(OldClsid)),
            Arg.Any<RegistryView>(),
            false);
    }

    [Fact]
    public void Register_BothModes_UseSameKeyList()
    {
        var allUsersKeys = new List<string>();
        var currentUserKeys = new List<string>();

        var mockAllUsers = Substitute.For<IRegistryOperations>();
        mockAllUsers.CreateSubKey(Arg.Any<RegistryHive>(), Arg.Any<string>(), Arg.Any<RegistryView>())
            .Returns(ci =>
            {
                allUsersKeys.Add(ci.ArgAt<string>(1));
                var mk = Substitute.For<IRegistryKeyWrapper>();
                return mk;
            });

        var mockCurrentUser = Substitute.For<IRegistryOperations>();
        mockCurrentUser.CreateSubKey(Arg.Any<RegistryHive>(), Arg.Any<string>(), Arg.Any<RegistryView>())
            .Returns(ci =>
            {
                currentUserKeys.Add(ci.ArgAt<string>(1));
                var mk = Substitute.For<IRegistryKeyWrapper>();
                return mk;
            });

        new ShellExtensionRegistrar(mockAllUsers, TestClsid)
            .Register(ShellExtensionRegistrar.InstallMode.AllUsers, TestDllPath);

        new ShellExtensionRegistrar(mockCurrentUser, TestClsid)
            .Register(ShellExtensionRegistrar.InstallMode.CurrentUser, TestDllPath);

        Assert.Equal(allUsersKeys, currentUserKeys);
    }
}
