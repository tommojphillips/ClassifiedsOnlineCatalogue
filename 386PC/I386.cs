using UnityEngine;
using MSCLoader;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using System.Collections.Generic;
using System;

namespace I386PC;

public class I386Command {
    public Func<bool> onEnter;
    public Func<bool> onUpdate;

    public I386Command() {
        onEnter = null;
        onUpdate = null;
    }
    public I386Command(Func<bool> enter) {
        onEnter = enter;
        onUpdate = null;
    }
    public I386Command(Func<bool> enter, Func<bool> update) {
        onEnter = enter;
        onUpdate = update;
    }
}

public class I386Diskette {
    internal FsmString _exe;
    internal FsmFloat _kb;

    internal FsmString _tag_exe;
    internal FsmString _tag_in_drive;
    internal FsmString _tag_kb;
    internal FsmString _tag_pos;
    
    internal GameObject _gameObject;

    public GameObject gameObject {
        get => _gameObject;
        internal set => _gameObject = value;
    }

    public string exe {
        get => _exe.Value; 
        set => _exe.Value = value;
    }

    public float kb {
        get => _kb.Value; 
        set => _kb.Value = value;
    }

    public void LoadExe(string exe, float kb) {
        this.exe = exe;
        this.kb = kb;

        _tag_exe.Value = $"floppy_{exe}_exe";
        _tag_in_drive.Value = $"floppy_{exe}_in_drive";
        _tag_kb.Value = $"floppy_{exe}_kb";
        _tag_pos.Value = $"floppy_{exe}_pos";
    }

    public void SetTexture(Texture2D texture) {
        Transform mesh = gameObject.transform.Find("mesh");
        if (mesh == null) {
            return;
        }
        MeshRenderer renderer = mesh.GetComponent<MeshRenderer>();
        if (renderer == null) {
            return;
        }
        renderer.materials[1].mainTexture = texture;
    }
}

public class I386 {
    public GameObject gameObject;
    public Transform transform;

    /// <summary>
    /// The Dial Up speed in bits per second
    /// </summary>
    public float baud { 
        get => baudFloat.Value;
        set => baudFloat.Value = value;
    }

    /// <summary>
    /// The command arguments
    /// </summary>
    public string[] argv;

    internal PlayMakerFSM commandFsm;

    I386Command currentCommand;
    FsmFloat baudFloat;
    FsmString exeString;
    FsmString commandString;
    FsmString oldString;
    FsmString textString;
    FsmString errorString;
    Transform pos;
    TextMesh consoleText;
    Dictionary<string, I386Command> commands;
    FsmString saveFile;

    internal I386() {
        commands = new Dictionary<string, I386Command>();

        gameObject = GameObject.Find("COMPUTER");
        transform = gameObject.transform;

        pos = transform.Find("SYSTEM/POS");
        Transform commandTransform = transform.Find("SYSTEM/POS/Command");
        commandFsm = commandTransform.GetPlayMaker("Typer");
        FsmState softwareListState = commandFsm.GetState("Software list");
        commandFsm.FsmInject("Software list", onFindCommand, false);

        FsmState customCommandState = commandFsm.AddState("CustomCommand");
        customCommandState.AddTransition("CLOSE", "State 2");
        //customCommandState.AddTransition("FINISHED", "Write new line");
        customCommandState.AddTransition("FINISHED", "Player input");

        commandFsm.FsmInject("CustomCommand", onCustomCommandEnter, false);
        commandFsm.FsmInject("CustomCommand", onCustomCommandUpdate, true);

        softwareListState.AddTransition("CUSTOM", "CustomCommand");

        FsmState driveMemState = commandFsm.GetState("Drive mem");
        driveMemState.AddTransition("CUSTOM", "Player input");
        commandFsm.FsmInject("Drive mem", onCheckCommandExists, false, 1);

        FsmState diskMemState = commandFsm.GetState("Disk mem");
        diskMemState.AddTransition("CUSTOM", "Player input");
        commandFsm.FsmInject("Disk mem", onCheckCommandExists, false, 2);

        exeString = commandFsm.GetVariable<FsmString>("EXE");
        commandString = commandFsm.GetVariable<FsmString>("Command");
        errorString = commandFsm.GetVariable<FsmString>("Error");
        textString = commandFsm.GetVariable<FsmString>("Text");
        oldString = commandFsm.GetVariable<FsmString>("Old");
        baudFloat = commandFsm.GetVariable<FsmFloat>("Baud");
        consoleText = commandTransform.GetComponent<TextMesh>();

        commandFsm.FsmInject("Init", onInit, false, 4);

        saveFile = new FsmString("i386");
        saveFile.Value = "i386.txt";
    }

    /// <summary>
    /// Get the time it would take to download x bytes at baud speed.
    /// </summary>
    /// <param name="bytes">Number of bytes</param>
    public float GetDownloadTime(int bytes) {
        return bytes / GetBps();
    }
    /// <summary>
    /// Get Dial Up speed in Bytes per second
    /// </summary>
    public float GetBps() {
        // 10 bits per byte (8N1)
        return baudFloat.Value / 10f;
    }

    /// <summary>
    /// Add a commmand to the i386 PC
    /// </summary>
    /// <param name="name">The command name</param>
    /// <param name="command">The command</param>
    public void AddCommand(string name, I386Command command) {
        if (commands.ContainsKey(name)) {
            ModConsole.Log($"[I386] Command {name} Modified");
            commands[name] = command;
        }
        else {
            ModConsole.Log($"[I386] Command {name} Added");
            commands.Add(name, command);
        }
    }

    /// <summary>
    /// Create new Diskette
    /// </summary>
    public I386Diskette CreateDiskette(Vector3 defaultPosition = default, Vector3 defaultEulerAngles = default) {
        I386Diskette diskette = new I386Diskette();
        GameObject prefab = GameObject.Find("diskette(itemx)");
        if (prefab == null) {
            return null;
        }

        GameObject g = GameObject.Instantiate(prefab);
        g.name = "diskette(itemx)";
        g.transform.position = defaultPosition;
        g.transform.eulerAngles = defaultEulerAngles;
        PlayMakerFSM fsm = g.GetPlayMaker("Use");
        if (fsm == null) {
            return null;
        }

        diskette._kb = fsm.GetVariable<FsmFloat>("KB");
        diskette._exe = fsm.GetVariable<FsmString>("EXE");
        diskette._tag_exe = fsm.GetVariable<FsmString>("UniqueTagEXE");
        diskette._tag_in_drive = fsm.GetVariable<FsmString>("UniqueTagInDrive");
        diskette._tag_kb = fsm.GetVariable<FsmString>("UniqueTagKB");
        diskette._tag_pos = fsm.GetVariable<FsmString>("UniqueTagPos");
        
        diskette.gameObject = g;

        FsmState s1 = fsm.GetState("Load");
        (s1.Actions[0] as LoadTransform).saveFile = saveFile;
        (s1.Actions[1] as LoadBool).saveFile = saveFile;
        (s1.Actions[2] as LoadString).saveFile = saveFile;
        (s1.Actions[3] as LoadFloat).saveFile = saveFile;

        FsmState s2 = fsm.GetState("Save");
        (s2.Actions[0] as SaveTransform).saveFile = saveFile;
        (s2.Actions[1] as SaveBool).saveFile = saveFile;
        (s2.Actions[2] as SaveString).saveFile = saveFile;
        (s2.Actions[3] as SaveFloat).saveFile = saveFile;

        FsmState s3 = fsm.GetState("State 4");
        (s3.Actions[1] as Exists).saveFile = saveFile;

        return diskette;
    }

    /// <summary>
    /// POS Write new line to console
    /// </summary>
    public void POS_NewLine() {
        pos.localPosition = Vector3.zero;
        oldString.Value = textString.Value + "\n";
        textString.Value = oldString.Value;
        consoleText.text = oldString.Value;
    }
    /// <summary>
    /// POS Write message with new line to console
    /// </summary>
    /// <param name="text">The text to write</param>
    public void POS_WriteNewLine(string text) {
        pos.localPosition = Vector3.zero;
        oldString.Value = textString.Value + text + "\n";
        commandString.Value = string.Empty;
        errorString.Value = string.Empty;
        textString.Value = oldString.Value;
        consoleText.text = oldString.Value;
    }
    /// <summary>
    /// POS Write message to console
    /// </summary>
    /// <param name="text">The text to write</param>
    public void POS_Write(string text) {
        pos.localPosition = Vector3.zero;
        oldString.Value = textString.Value + text;
        commandString.Value = string.Empty;
        errorString.Value = string.Empty;
        textString.Value = oldString.Value;
        consoleText.text = oldString.Value;
    }
    /// <summary>
    /// POS Clear Screen
    /// </summary>
    public void POS_ClearScreen() {
        pos.localPosition = Vector3.zero;
        commandString.Value = string.Empty;
        errorString.Value = string.Empty;
        oldString.Value = "\n";
        textString.Value = string.Empty;
        consoleText.text = oldString.Value;
    }

    private void exitCommand() {
        ModConsole.Log($"[I386] Command {commandString.Value} Finished");
        //commandFsm.SendEvent("CLOSE");
        commandFsm.SendEvent("FINISHED");
        currentCommand = null;
    }

    // Callbacks/Events

    private void onInit() {
        POS_NewLine();
    }
    private void onCheckCommandExists() {
        if (commandString.Value == string.Empty) {
            commandFsm.SendEvent("CUSTOM");
            return;
        }

        argv = commandString.Value.Split(' ');
        if (argv.Length > 0) {
            exeString.Value = argv[0];
        }

        /*argv = commandString.Value.Split(' ');
        if (commands.TryGetValue(argv[0], out currentCommand)) {
            ModConsole.Log($"[I386] Command {commandString.Value} Exists");
            commandFsm.SendEvent("LOAD");
        }*/
    }
    private void onFindCommand() {
        argv = commandString.Value.Split(' ');
        if (commands.TryGetValue(argv[0], out currentCommand)) {
            ModConsole.Log($"[I386] Command {commandString.Value} Started");
            commandFsm.SendEvent("CUSTOM");
        }
    }
    private void onCustomCommandUpdate() {
        bool t = true;
        if (currentCommand?.onUpdate != null) {
            t = currentCommand.onUpdate.Invoke();
        }

        if (t) {
            exitCommand();
        }
    }
    private void onCustomCommandEnter() {
        bool t = false;
        if (currentCommand?.onEnter != null) {
            t = currentCommand.onEnter.Invoke();
        }
        
        if (t) {
            exitCommand();
        }
    }
}
