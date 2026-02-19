using UnityEngine;
using MSCLoader;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using System.Collections.Generic;
using System;

namespace I386API;

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
    internal MeshRenderer _renderer;
    internal MeshFilter _filter;

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
        if (_renderer == null) {
            return;
        }

        _renderer.materials[1].mainTexture = texture;
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
    /// Is Modem connected to telephone outlet
    /// </summary>
    public bool modemConnected => modemCord.Value && phonePaid.Value;

    /// <summary>
    /// The command arguments
    /// </summary>
    public string[] argv;

    internal PlayMakerFSM commandFsm;

    I386Command currentCommand;
    Dictionary<string, I386Command> commands;
    Transform pos;
    TextMesh consoleText;
    FsmFloat baudFloat;
    FsmString exeString;
    FsmString commandString;
    FsmString oldString;
    FsmString textString;
    FsmString errorString;
    FsmString saveFile;
    FsmBool playerComputer;
    FsmBool modemCord;
    FsmBool phonePaid;
    
    Texture2D floppyBlankTexture;
    Material floppyBlankMaterial;
    GameObject floppyPrefab;

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

        playerComputer = PlayMakerGlobals.Instance.Variables.FindFsmBool("PlayerComputer");

        GameObject modemCord_go = GameObject.Find("YARD/Building/LIVINGROOM/Telephone 1/Cord");
        PlayMakerFSM modemCord_fsm = modemCord_go.GetPlayMaker("Use");
        modemCord = modemCord_fsm.GetVariable<FsmBool>("CordModem");

        GameObject phoneBill1_go = GameObject.Find("Systems/PhoneBills1");
        PlayMakerFSM phoneBill1_fsm = phoneBill1_go.GetPlayMaker("Data");
        phonePaid = phoneBill1_fsm.GetVariable<FsmBool>("PhonePaid");

        floppyBlankTexture = new Texture2D(2048, 2048);
        floppyBlankTexture.LoadImage(Resources.FLOPPY_BLANK);
        floppyBlankTexture.name = "FLOPPY_IMAGE";

        floppyPrefab = GameObject.Find("diskette(itemx)");
        MeshRenderer renderer = floppyPrefab.transform.Find("mesh").GetComponent<MeshRenderer>();
        Material[] mats = renderer.materials;
        floppyBlankMaterial = new Material(mats[1]);
        floppyBlankMaterial.name = "FLOPPY_IMAGE";
        floppyBlankMaterial.mainTexture = floppyBlankTexture;
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
            commands[name] = command;
        }
        else {
            commands.Add(name, command);
        }
    }

    /// <summary>
    /// Create new Diskette
    /// </summary>
    public I386Diskette CreateDiskette(Vector3 defaultPosition = default, Vector3 defaultEulerAngles = default) {
        I386Diskette diskette = new I386Diskette();
        
        GameObject g = GameObject.Instantiate(floppyPrefab);
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

        Transform mesh = diskette.gameObject.transform.Find("mesh");
        if (mesh != null) {
            diskette._renderer = mesh.GetComponent<MeshRenderer>();
            diskette._filter = mesh.GetComponent<MeshFilter>();
            if (diskette._renderer != null && diskette._filter != null) {

                Mesh original = diskette._filter.sharedMesh;
                Mesh m = GameObject.Instantiate(original);
                diskette._filter.mesh = m;

                int[] triangles = m.GetTriangles(1);
                Vector2[] uvs = m.uv;

                Rect atlasRect = new Rect(0.3125f, 0.125f, 0.0625f, 0.0625f);

                HashSet<int> usedVerts = new HashSet<int>(triangles);

                foreach (int vertIndex in usedVerts) {
                    Vector2 uv = uvs[vertIndex];

                    uv.x = (uv.x - atlasRect.x) / atlasRect.width;
                    uv.y = (uv.y - atlasRect.y) / atlasRect.height;

                    uvs[vertIndex] = uv;
                }

                m.uv = uvs;

                Material[] mats = diskette._renderer.materials;                
                mats[1] = floppyBlankMaterial;
                diskette._renderer.materials = mats;
            }
        }

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

    /// <summary>
    /// Get key
    /// </summary>
    /// <param name="key">key code</param>
    public bool GetKey(KeyCode key) {
        return playerComputer.Value && Input.GetKey(key);
    }
    /// <summary>
    /// Get key down
    /// </summary>
    /// <param name="key">key code</param>
    public bool GetKeyDown(KeyCode key) {
        return playerComputer.Value && Input.GetKeyDown(key);
    }
    /// <summary>
    /// Get key up
    /// </summary>
    /// <param name="key">key code</param>
    public bool GetKeyUp(KeyCode key) {
        return playerComputer.Value && Input.GetKeyUp(key);
    }

    private void exitCommand() {
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
    }
    private void onFindCommand() {
        argv = commandString.Value.Split(' ');
        if (commands.TryGetValue(argv[0], out currentCommand)) {
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
