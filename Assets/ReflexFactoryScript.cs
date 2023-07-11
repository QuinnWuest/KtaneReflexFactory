using System.Collections;
using System.Linq;
using UnityEngine;
using KModkit;
using System.Text.RegularExpressions;
using System;

public class ReflexFactoryScript : MonoBehaviour {

    public KMAudio audio;
    public KMAudio.KMAudioRef music;
    public KMBombInfo bomb;
    public KMColorblindMode cbMode;
    public KMSelectable[] buttons;
    public Transform[] hatchDoors;
    public TextMesh display;
    public GameObject[] buttonObjs;
    public Material[] buttonMats;

    int[] numOfBtns = new int[8];
    int[] btnTypes = new int[8];
    int[] moves = new int[7];
    string[] cbLabels = { "R", "O", "Y", "G", "B", "P", "W", "E", "N" };
    string[] btnTypeString = { "", "", "", "", "", "", "", "", "" };
    string[][] labels = new string[][] { new string[] { "ABORT", "BUTTON", "DETONATE" }, new string[] { "HALT", "HOLD", "PRESS" }, new string[] { "PUSH", "STOP", "TAP" } };
    string arrows = "↖↗↘↙←↑→↓";
    bool sequence;
    bool correct;
    bool fail;
    bool lightsOn;
    int stage = -1;

    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;

    void Awake()
    {
        moduleId = moduleIdCounter++;
        foreach (KMSelectable obj in buttons)
        {
            KMSelectable pressed = obj;
            pressed.OnInteract += delegate () { PressButton(pressed); return false; };
        }
    }

    void Start()
    {
        for (int i = 0; i < 4; i++)
            buttonObjs[i].SetActive(false);
        string logger = "";
        for (int i = 0; i < 7; i++)
        {
            moves[i] = UnityEngine.Random.Range(0, 8);
            logger += arrows[moves[i]];
        }
        Debug.LogFormat("[Reflex Factory #{0}] Arrows: {1}", moduleId, logger);
        Generate();
        GetComponent<KMBombModule>().OnActivate += LightsOn;
    }

    void LightsOn()
    {
        hatchDoors[0].gameObject.SetActive(true);
        hatchDoors[1].gameObject.SetActive(true);
        for (int i = 0; i < 7; i++)
            display.text += arrows[moves[i]];
        lightsOn = true;
    }

    void Generate()
    {
        int moveIndex = bomb.GetSerialNumberNumbers().Last();
        if (moveIndex > 0)
            moveIndex -= 1;
        int moveX = moveIndex % 3;
        int moveY = moveIndex / 3;
        for (int i = 0; i < 8; i++)
        {
            if (i > 0)
            {
                switch (arrows[moves[i - 1]])
                {
                    case '↖':
                        moveY -= 1;
                        moveX -= 1;
                        if (moveY < 0)
                            moveY = 2;
                        if (moveX < 0)
                            moveX = 2;
                        break;
                    case '↗':
                        moveY -= 1;
                        moveX += 1;
                        if (moveY < 0)
                            moveY = 2;
                        if (moveX > 2)
                            moveX = 0;
                        break;
                    case '↘':
                        moveY += 1;
                        moveX += 1;
                        if (moveY > 2)
                            moveY = 0;
                        if (moveX > 2)
                            moveX = 0;
                        break;
                    case '↙':
                        moveY += 1;
                        moveX -= 1;
                        if (moveY > 2)
                            moveY = 0;
                        if (moveX < 0)
                            moveX = 2;
                        break;
                    case '←':
                        moveX -= 1;
                        if (moveX < 0)
                            moveX = 2;
                        break;
                    case '↑':
                        moveY -= 1;
                        if (moveY < 0)
                            moveY = 2;
                        break;
                    case '→':
                        moveX += 1;
                        if (moveX > 2)
                            moveX = 0;
                        break;
                    default:
                        moveY += 1;
                        if (moveY > 2)
                            moveY = 0;
                        break;
                }
            }
            numOfBtns[i] = UnityEngine.Random.Range(3, 5);
            btnTypes[i] = UnityEngine.Random.Range(0, 2);
            if (btnTypes[i] == 0)
            {
                for (int j = 0; j < numOfBtns[i]; j++)
                {
                    if (j == 0)
                        btnTypeString[i] += labels[moveY][moveX] + "|";
                    else
                    {
                        int choice = UnityEngine.Random.Range(0, 9);
                        while (btnTypeString[i].Contains(labels[choice / 3][choice % 3]))
                            choice = UnityEngine.Random.Range(0, 9);
                        if (j == numOfBtns[i] - 1)
                            btnTypeString[i] += labels[choice / 3][choice % 3];
                        else
                            btnTypeString[i] += labels[choice / 3][choice % 3] + "|";
                    }
                }
            }
            else
            {
                for (int j = 0; j < numOfBtns[i]; j++)
                {
                    if (j == 0)
                        btnTypeString[i] += buttonMats[moveY * 3 + moveX + 1].name + "|";
                    else
                    {
                        int choice = UnityEngine.Random.Range(1, 10);
                        while (btnTypeString[i].Contains(buttonMats[choice].name))
                            choice = UnityEngine.Random.Range(1, 10);
                        if (j == numOfBtns[i] - 1)
                            btnTypeString[i] += buttonMats[choice].name;
                        else
                            btnTypeString[i] += buttonMats[choice].name + "|";
                    }
                }
            }
        }
        for (int i = 0; i < 8; i++)
            Debug.LogFormat("[Reflex Factory #{0}] Button set {1} has {2}: {3}", moduleId, i + 1, btnTypes[i] == 0 ? "labels" : "colors", btnTypeString[i].Split('|').Join().ToUpper());
    }

    void PressButton(KMSelectable pressed)
    {
        if (moduleSolved != true && lightsOn != false)
        {
            int index = Array.IndexOf(buttons, pressed);
            if (index == 0 && !sequence)
            {
                pressed.AddInteractionPunch();
                audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, pressed.transform);
                sequence = true;
                StartCoroutine(DisplayChanger());
                StartCoroutine(TestSequence());
            }
            else if (index != 0 && sequence)
            {
                pressed.AddInteractionPunch();
                audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, pressed.transform);
                if (index == 1)
                    correct = true;
                else
                {
                    fail = true;
                    Debug.LogFormat("[Reflex Factory #{0}] An incorrect button was pressed, resetting test sequence...", moduleId);
                }
            }
        }
    }

    IEnumerator DisplayChanger()
    {
        display.transform.localPosition = new Vector3(-0.015f, 0.0151f, 0.067f);
        display.text = "Get Ready";
        for (int i = 0; i < 7; i++)
        {
            yield return new WaitForSecondsRealtime(0.375f);
            if (display.text.Contains("..."))
                display.text = "Get Ready";
            else
                display.text += ".";
        }
    }

    IEnumerator TestSequence()
    {
        music = audio.HandlePlaySoundAtTransformWithRef("sequence", transform, false);
        yield return new WaitForSecondsRealtime(3);
        display.text = "";
        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                if (j < numOfBtns[i])
                {
                    buttonObjs[j].SetActive(true);
                    if (btnTypes[i] == 0)
                    {
                        buttonObjs[j].GetComponentInChildren<MeshRenderer>().material = buttonMats[0];
                        buttonObjs[j].GetComponentInChildren<TextMesh>().text = btnTypeString[i].Split('|')[j];
                        if (buttonObjs[j].GetComponentInChildren<TextMesh>().text.Length > 5)
                            buttonObjs[j].GetComponentInChildren<TextMesh>().transform.localScale = new Vector3(.00001f, .00001f, 1);
                        else
                            buttonObjs[j].GetComponentInChildren<TextMesh>().transform.localScale = new Vector3(.000013f, .000013f, 1);
                    }
                    else
                    {
                        int index = -1;
                        for (int k = 1; k < buttonMats.Length; k++)
                        {
                            if (btnTypeString[i].Split('|')[j] == buttonMats[k].name)
                            {
                                buttonObjs[j].GetComponentInChildren<MeshRenderer>().material = buttonMats[k];
                                index = k - 1;
                                break;
                            }
                        }
                        if (cbMode.ColorblindModeActive)
                        {
                            buttonObjs[j].GetComponentInChildren<TextMesh>().transform.localScale = new Vector3(.000013f, .000013f, 1);
                            buttonObjs[j].GetComponentInChildren<TextMesh>().text = cbLabels[index];
                        }
                        else
                            buttonObjs[j].GetComponentInChildren<TextMesh>().text = "";
                    }
                }
                else
                    buttonObjs[j].SetActive(false);
                redo:
                buttonObjs[j].transform.localPosition = new Vector3(UnityEngine.Random.Range(-0.06f, 0.03f), -0.008f, UnityEngine.Random.Range(-0.06f, 0.03f));
                for (int k = 0; k < j; k++)
                {
                    if (Vector3.Distance(buttonObjs[j].transform.localPosition, buttonObjs[k].transform.localPosition) < 0.028f)
                        goto redo;
                }
            }
            StartCoroutine(Hatch(true));
            yield return new WaitForSecondsRealtime(.75f);
            StartCoroutine(Hatch(false));
            yield return new WaitForSecondsRealtime(.8f);
        }
        yield return new WaitForSecondsRealtime(.2f);
        Debug.LogFormat("[Reflex Factory #{0}] Test sequence passed, module solved", moduleId);
        GetComponent<KMBombModule>().HandlePass();
        moduleSolved = true;
        display.text = "GG";
    }

    IEnumerator Hatch(bool open)
    {
        if (open)
            stage++;
        Vector3 oldPosTop = hatchDoors[0].localPosition;
        Vector3 oldPosBottom = hatchDoors[1].localPosition;
        Vector3 newPosTop = hatchDoors[0].localPosition + new Vector3(0f, 0f, open ? 0.07f : -0.07f);
        Vector3 newPosBottom = hatchDoors[1].localPosition + new Vector3(0f, 0f, open ? -0.07f : 0.07f);
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * (open ? 2f : 5f);
            yield return null;
            hatchDoors[0].localPosition = Vector3.Lerp(oldPosTop, newPosTop, t);
            hatchDoors[1].localPosition = Vector3.Lerp(oldPosBottom, newPosBottom, t);
        }
        if (!open)
        {
            if (!correct || fail)
            {
                if (!fail)
                    Debug.LogFormat("[Reflex Factory #{0}] The correct button was not pressed in time, resetting test sequence...", moduleId);
                music.StopSound();
                audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.Strike, transform);
                for (int i = 0; i < 7; i++)
                    display.text += arrows[moves[i]];
                display.transform.localPosition = new Vector3(-0.015f, 0.0151f, 0.069f);
                for (int i = 0; i < 8; i++)
                    btnTypeString[i] = "";
                sequence = false;
                stage = -1;
                Generate();
                StopAllCoroutines();
            }
            correct = false;
            fail = false;
            for (int i = 0; i < 4; i++)
                buttonObjs[i].SetActive(false);
        }
    }

    //twitch plays
    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} submit red/abort green/halt... [Starts the test sequence and presses the button that is present with either property] | 8 pairs of properties must be included in a submit command";
    #pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string command)
    {
        string[] parameters = command.Split(' ');
        if (Regex.IsMatch(parameters[0], @"^\s*submit\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            if (parameters.Length < 9)
                yield return "sendtochaterror Please specify 8 pairs of properties!";
            else if (parameters.Length > 9)
                yield return "sendtochaterror Too many parameters!";
            else
            {
                string[] valids = { "red", "orange", "yellow", "green", "blue", "purple", "white", "grey", "brown", "abort", "button", "detonate", "halt", "hold", "press", "push", "stop", "tap" };
                for (int i = 1; i < 9; i++)
                {
                    if (!parameters[i].Contains("/"))
                    {
                        yield return "sendtochaterror!f '" + parameters[i] + "' is an invalid pair of properties!";
                        yield break;
                    }
                    string[] properties = parameters[i].Split('/');
                    if (properties.Length != 2 || !valids.Contains(properties[0].ToLower()) || !valids.Contains(properties[1].ToLower()))
                    {
                        yield return "sendtochaterror!f '" + parameters[i] + "' is an invalid pair of properties!";
                        yield break;
                    }
                }
                yield return null;
                buttons[0].OnInteract();
                for (int i = 1; i < 9; i++)
                {
                    while (!buttonObjs[1].activeSelf) yield return null;
                    string[] shownProperties = btnTypeString[stage].ToLower().Split('|');
                    string[] properties = parameters[i].ToLower().Split('/');
                    bool good = false;
                    for (int j = 0; j < numOfBtns[stage]; j++)
                    {
                        if (shownProperties[j] == properties[0] || shownProperties[j] == properties[1])
                        {
                            buttons[j + 1].OnInteract();
                            good = true;
                            break;
                        }
                    }
                    if (fail || !good)
                        yield break;
                    else if (good && !fail && i == 8)
                        yield return "solve";
                    if (i != 8)
                        while (buttonObjs[1].activeSelf) yield return null;
                }
            }
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        while (!lightsOn || fail) yield return true;
        if (!sequence)
            buttons[0].OnInteract();
        while (stage < 7)
        {
            while (!buttonObjs[1].activeSelf) yield return null;
            if (!correct)
                buttons[1].OnInteract();
            if (stage != 7)
                while (buttonObjs[1].activeSelf) yield return null;
        }
        while (!moduleSolved) yield return true;
    }
}