using Godot;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

[Tool]
public class DialogueRunner : Control {
    internal enum CommandDispatchResult {
        Success,
        Failed,
        NotFound,
    }

    public YarnProject yarnProject;
    internal VariableStorageBehaviour _variableStorage;

    public VariableStorageBehaviour VariableStorage {
        get => _variableStorage;
        set {
            _variableStorage = value;
            if (_dialogue != null) {
                _dialogue.VariableStorage = value;
            }
        }
    }

    public DialogueViewBase[] dialogueViews;

    public string startNode = Yarn.Dialogue.DefaultStartNodeName;

    public bool startAutomatically = true;

    [Export] public bool automaticallyContinueLines;

    public bool runSelectedOptionAsLine;

    public LineProviderBehaviour lineProvider;

    [Export] public bool verboseLogging = true;

    public bool IsDialogueRunning { get; set; }

    [Signal]
    public delegate void onNodeStart(string str);

    [Signal]
    public delegate void onNodeComplete(string str);

    [Signal]
    public delegate void onDialogueComplete();

    [Signal]
    public delegate void onCommand(string str);

    public string CurrentNodeName => Dialogue.CurrentNode;

    public Yarn.Dialogue Dialogue => _dialogue ?? (_dialogue = CreateDialogueInstance());

    private bool IsOptionSelectionAllowed = false;

    private List<IEnumerator> coroutines = new List<IEnumerator>();

    public void SetProject(YarnProject newProject) {
        // Load all of the commands and functions from the assemblies that
        // this project wants to load from.
        ActionManager.AddActionsFromAssemblies(newProject.searchAssembliesForActions);

        Dialogue.SetProgram(newProject.GetProgram());
        lineProvider.YarnProject = newProject;
    }

    public void SetInitialVariables(bool overrideExistingValues = false) {
        if (yarnProject == null) {
            GD.PrintErr("Unable to set default values, there is no project set");
            return;
        }

        // grabbing all the initial values from the program and inserting them into the storage
        // we first need to make sure that the value isn't already set in the storage
        var values = yarnProject.GetProgram().InitialValues;
        foreach (var pair in values) {
            if (!overrideExistingValues && VariableStorage.Contains(pair.Key)) {
                continue;
            }
            var value = pair.Value;
            switch (value.ValueCase) {
                case Yarn.Operand.ValueOneofCase.StringValue: {
                        VariableStorage.SetValue(pair.Key, value.StringValue);
                        break;
                    }
                case Yarn.Operand.ValueOneofCase.BoolValue: {
                        VariableStorage.SetValue(pair.Key, value.BoolValue);
                        break;
                    }
                case Yarn.Operand.ValueOneofCase.FloatValue: {
                        VariableStorage.SetValue(pair.Key, value.FloatValue);
                        break;
                    }
                default: {
                        GD.PrintErr($"{pair.Key} is of an invalid type: {value.ValueCase}");
                        break;
                    }
            }
        }
    }

    private IEnumerator StartCoroutine(IEnumerator enumerator) {
        if (enumerator.MoveNext()) {
            coroutines.Add(enumerator);
        }

        return null;
    }

    public override void _Process(float delta) {
        List<IEnumerator> dead = new List<IEnumerator>();
        foreach (var c in coroutines) {
            if (!c.MoveNext()) {
                dead.Add(c);
            }
        }

        foreach (var c in dead) {
            coroutines.Remove(c);
        }

        dead.Clear();
    }

    public void StartDialogue(string startNode) {
        // If the dialogue is currently executing instructions, then
        // calling ContinueDialogue() at the end of this method will
        // cause confusing results. Report an error and stop here.
        if (Dialogue.IsActive) {
            GD.PrintErr($"Can't start dialogue from node {startNode}: the dialogue is currently in the middle of running. Stop the dialogue first.");
            return;
        }

        // Stop any processes that might be running already
        foreach (var dialogueView in dialogueViews) {
            if (dialogueView == null || dialogueView.Visible == false) {
                continue;
            }

            dialogueView.StopAllCoroutines();
        }

        // Get it going

        // Mark that we're in conversation.
        IsDialogueRunning = true;

        // Signal that we're starting up.
        foreach (var dialogueView in dialogueViews) {
            if (dialogueView == null || dialogueView.Visible == false) continue;

            dialogueView.DialogueStarted();
        }

        // Request that the dialogue select the current node. This
        // will prepare the dialogue for running; as a side effect,
        // our prepareForLines delegate may be called.
        Dialogue.SetNode(startNode);

        if (lineProvider.LinesAvailable == false) {
            // The line provider isn't ready to give us our lines
            // yet. We need to start a coroutine that waits for
            // them to finish loading, and then runs the dialogue.
            StartCoroutine(ContinueDialogueWhenLinesAvailable());
        }
        else {
            ContinueDialogue();
        }
    }

    private IEnumerator ContinueDialogueWhenLinesAvailable() {
        // Wait until lineProvider.LinesAvailable becomes true
        while (lineProvider.LinesAvailable == false) {
            yield return null;
        }

        // And then run our dialogue.
        ContinueDialogue();
    }

    [Obsolete("Use " + nameof(StartDialogue) + "(nodeName) instead.")]
    public void ResetDialogue(string nodeName = null) {
        nodeName = nodeName ?? startNode ?? CurrentNodeName ?? throw new ArgumentNullException($"Cannot reset dialogue: couldn't figure out a node to restart the dialogue from.");

        StartDialogue(nodeName);
    }

    public void Clear() {
        
        //Assert.IsFalse(IsDialogueRunning, "You cannot clear the dialogue system while a dialogue is running.");
        Dialogue.UnloadAll();
    }

    public void Stop() {
        IsDialogueRunning = false;
        Dialogue.Stop();
    }

    public bool NodeExists(string nodeName) => Dialogue.NodeExists(nodeName);

    public IEnumerable<string> GetTagsForNode(String nodeName) => Dialogue.GetTagsForNode(nodeName);

    private void AddCommandHandler(string commandName, Delegate handler) {
        if (commandHandlers.ContainsKey(commandName)) {
            GD.PrintErr($"Cannot add a command handler for {commandName}: one already exists");
            return;
        }
        commandHandlers.Add(commandName, handler);
    }

    /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
    public void AddCommandHandler(string commandName, System.Func<IEnumerator> handler) {
        AddCommandHandler(commandName, (Delegate)handler);
    }

    /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
    public void AddCommandHandler<T1>(string commandName, System.Func<T1, IEnumerator> handler) {
        AddCommandHandler(commandName, (Delegate)handler);
    }

    /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
    public void AddCommandHandler<T1, T2>(string commandName, System.Func<T1, T2, IEnumerator> handler) {
        AddCommandHandler(commandName, (Delegate)handler);
    }

    /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
    public void AddCommandHandler<T1, T2, T3>(string commandName, System.Func<T1, T2, T3, IEnumerator> handler) {
        AddCommandHandler(commandName, (Delegate)handler);
    }

    /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
    public void AddCommandHandler<T1, T2, T3, T4>(string commandName, System.Func<T1, T2, T3, T4, IEnumerator> handler) {
        AddCommandHandler(commandName, (Delegate)handler);
    }

    /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
    public void AddCommandHandler<T1, T2, T3, T4, T5>(string commandName, System.Func<T1, T2, T3, T4, T5, IEnumerator> handler) {
        AddCommandHandler(commandName, (Delegate)handler);
    }

    /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
    public void AddCommandHandler<T1, T2, T3, T4, T5, T6>(string commandName, System.Func<T1, T2, T3, T4, T5, T6, IEnumerator> handler) {
        AddCommandHandler(commandName, (Delegate)handler);
    }

    /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
    public void AddCommandHandler(string commandName, System.Action handler) {
        AddCommandHandler(commandName, (Delegate)handler);
    }

    /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
    public void AddCommandHandler<T1>(string commandName, System.Action<T1> handler) {
        AddCommandHandler(commandName, (Delegate)handler);
    }

    /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
    public void AddCommandHandler<T1, T2>(string commandName, System.Action<T1, T2> handler) {
        AddCommandHandler(commandName, (Delegate)handler);
    }

    /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
    public void AddCommandHandler<T1, T2, T3>(string commandName, System.Action<T1, T2, T3> handler) {
        AddCommandHandler(commandName, (Delegate)handler);
    }

    /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
    public void AddCommandHandler<T1, T2, T3, T4>(string commandName, System.Action<T1, T2, T3, T4> handler) {
        AddCommandHandler(commandName, (Delegate)handler);
    }

    /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
    public void AddCommandHandler<T1, T2, T3, T4, T5>(string commandName, System.Action<T1, T2, T3, T4, T5> handler) {
        AddCommandHandler(commandName, (Delegate)handler);
    }

    /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
    public void AddCommandHandler<T1, T2, T3, T4, T5, T6>(string commandName, System.Action<T1, T2, T3, T4, T5, T6> handler) {
        AddCommandHandler(commandName, (Delegate)handler);
    }

    public void RemoveCommandHandler(string commandName) {
        commandHandlers.Remove(commandName);
    }

    private void AddFunction(string name, Delegate implementation) {
        if (Dialogue.Library.FunctionExists(name)) {
            GD.PrintErr($"Cannot add function {name}: one already exists");
            return;
        }

        Dialogue.Library.RegisterFunction(name, implementation);
    }

    /// <inheritdoc cref="AddFunction(string, Delegate)" />
    /// <typeparam name="TResult">The type of the value that the function should return.</typeparam>
    public void AddFunction<TResult>(string name, System.Func<TResult> implementation) {
        AddFunction(name, (Delegate)implementation);
    }

    /// <inheritdoc cref="AddFunction{TResult}(string, Func{TResult})" />
    /// <typeparam name="T1">The type of the first parameter to the function.</typeparam>
    public void AddFunction<TResult, T1>(string name, System.Func<TResult, T1> implementation) {
        AddFunction(name, (Delegate)implementation);
    }

    /// <inheritdoc cref="AddFunction{TResult,T1}(string, Func{TResult,T1})" />
    /// <typeparam name="T2">The type of the second parameter to the function.</typeparam>
    public void AddFunction<TResult, T1, T2>(string name, System.Func<TResult, T1, T2> implementation) {
        AddFunction(name, (Delegate)implementation);
    }

    /// <inheritdoc cref="AddFunction{TResult,T1,T2}(string, Func{TResult,T1,T2})" />
    /// <typeparam name="T3">The type of the third parameter to the function.</typeparam>
    public void AddFunction<TResult, T1, T2, T3>(string name, System.Func<TResult, T1, T2, T3> implementation) {
        AddFunction(name, (Delegate)implementation);
    }

    /// <inheritdoc cref="AddFunction{TResult,T1,T2,T3}(string, Func{TResult,T1,T2,T3})" />
    /// <typeparam name="T4">The type of the fourth parameter to the function.</typeparam>
    public void AddFunction<TResult, T1, T2, T3, T4>(string name, System.Func<TResult, T1, T2, T3, T4> implementation) {
        AddFunction(name, (Delegate)implementation);
    }

    /// <inheritdoc cref="AddFunction{TResult,T1,T2,T3,T4}(string, Func{TResult,T1,T2,T3,T4})" />
    /// <typeparam name="T5">The type of the fifth parameter to the function.</typeparam>
    public void AddFunction<TResult, T1, T2, T3, T4, T5>(string name, System.Func<TResult, T1, T2, T3, T4, T5> implementation) {
        AddFunction(name, (Delegate)implementation);
    }

    /// <inheritdoc cref="AddFunction{TResult,T1,T2,T3,T4,T5}(string, Func{TResult,T1,T2,T3,T4,T5})" />
    /// <typeparam name="T6">The type of the sixth parameter to the function.</typeparam>
    public void AddFunction<TResult, T1, T2, T3, T4, T5, T6>(string name, System.Func<TResult, T1, T2, T3, T4, T5, T6> implementation) {
        AddFunction(name, (Delegate)implementation);
    }

    public void RemoveFunction(string name) => Dialogue.Library.DeregisterFunction(name);

    public void SetDialogueViews(DialogueViewBase[] views) {
        dialogueViews = views;

        Action continueAction = OnViewUserIntentNextLine;
        foreach (var dialogueView in dialogueViews) {
            if (dialogueView == null) {
                GD.PrintErr("The 'Dialogue Views' field contains a NULL element.");
                continue;
            }

            dialogueView.onUserWantsLineContinuation = continueAction;
        }
    }

    #region Private Properties/Variables/Procedures

    internal LocalizedLine CurrentLine { get; private set; }

    private readonly HashSet<DialogueViewBase> ActiveDialogueViews = new HashSet<DialogueViewBase>();

    Action<int> selectAction;

    /// Maps the names of commands to action delegates.
    Dictionary<string, Delegate> commandHandlers = new Dictionary<string, Delegate>();

    private Yarn.Dialogue _dialogue;

    private Yarn.OptionSet currentOptions;

    public override void _Ready() {
        if (lineProvider == null) {
            // If we don't have a line provider, create a
            // TextLineProvider and make it use that.

            // Create the temporary line provider and the line database
            var lineProvider = new TextLineProvider();
            AddChild(lineProvider);

            // Let the user know what we're doing.
            if (verboseLogging) {
                GD.Print($"Dialogue Runner has no LineProvider; creating a {nameof(TextLineProvider)}.", this);
            }
        }

        if (dialogueViews.Length == 0) {
            GD.PrintErr($"Dialogue Runner doesn't have any dialogue views set up. No lines or options will be visible.");
        }

        // Give each dialogue view the continuation action, which
        // they'll call to pass on the user intent to move on to the
        // next line (or interrupt the current one).
        System.Action continueAction = OnViewUserIntentNextLine;
        foreach (var dialogueView in dialogueViews) {
            // Skip any null or disabled dialogue views
            if (dialogueView == null || dialogueView.Visible == false) {
                continue;
            }

            dialogueView.onUserWantsLineContinuation = continueAction;
        }

        if (yarnProject != null) {
            if (Dialogue.IsActive) {
                GD.PrintErr($"DialogueRunner wanted to load a Yarn Project in its Start method, but the Dialogue was already running one. The Dialogue Runner may not behave as you expect.");
            }

            // Load all of the commands and functions from the assemblies
            // that this project wants to load from.
            ActionManager.AddActionsFromAssemblies(yarnProject.searchAssembliesForActions);

            Dialogue.SetProgram(yarnProject.GetProgram());

            lineProvider.YarnProject = yarnProject;

            SetInitialVariables();

            if (startAutomatically) {
                StartDialogue(startNode);
            }
        }
    }

    Yarn.Dialogue CreateDialogueInstance() {
        if (VariableStorage == null) {
            // If we don't have a variable storage, create an
            // InMemoryVariableStorage and make it use that.

            VariableStorage = new InMemoryVariableStorage();
            AddChild(VariableStorage);

            // Let the user know what we're doing.
            if (verboseLogging) {
                GD.Print($"Dialogue Runner has no Variable Storage; creating a {nameof(InMemoryVariableStorage)}", this);
            }
        }

        // Create the main Dialogue runner, and pass our
        // variableStorage to it
        var dialogue = new Yarn.Dialogue(VariableStorage) {
            // Set up the logging system.
            LogDebugMessage = delegate (string message)
            {
                if (verboseLogging) {
                    GD.Print(message);
                }
            },
            LogErrorMessage = delegate (string message)
            {
                GD.PrintErr(message);
            },

            LineHandler = HandleLine,
            CommandHandler = HandleCommand,
            OptionsHandler = HandleOptions,
            NodeStartHandler = (node) =>
            {
                EmitSignal("onNodeStart", node);
            },
            NodeCompleteHandler = (node) =>
            {
                EmitSignal("onNodeComplete", node);
            },
            DialogueCompleteHandler = HandleDialogueComplete,
            PrepareForLinesHandler = PrepareForLines
        };

        ActionManager.RegisterFunctions(dialogue.Library);
        selectAction = SelectedOption;
        return dialogue;
    }

    void HandleOptions(Yarn.OptionSet options) {
        currentOptions = options;

        DialogueOption[] optionSet = new DialogueOption[options.Options.Length];
        for (int i = 0; i < options.Options.Length; i++) {
            // Localize the line associated with the option
            var localisedLine = lineProvider.GetLocalizedLine(options.Options[i].Line);
            var text = Yarn.Dialogue.ExpandSubstitutions(localisedLine.RawText, options.Options[i].Line.Substitutions);
            localisedLine.Text = Dialogue.ParseMarkup(text);

            optionSet[i] = new DialogueOption {
                TextID = options.Options[i].Line.ID,
                DialogueOptionID = options.Options[i].ID,
                Line = localisedLine,
                IsAvailable = options.Options[i].IsAvailable,
            };
        }

        // Don't allow selecting options on the same frame that we
        // provide them
        IsOptionSelectionAllowed = false;

        foreach (var dialogueView in dialogueViews) {
            if (dialogueView == null || dialogueView.Visible == false) continue;

            dialogueView.RunOptions(optionSet, selectAction);
        }

        IsOptionSelectionAllowed = true;
    }

    void HandleDialogueComplete() {
        IsDialogueRunning = false;
        foreach (var dialogueView in dialogueViews) {
            if (dialogueView == null || dialogueView.Visible == false) continue;

            dialogueView.DialogueComplete();
        }
        //onDialogueComplete.Invoke();
        EmitSignal("onDialogueComplete");
    }

    void HandleCommand(Yarn.Command command) {
        CommandDispatchResult dispatchResult;

        // Try looking in the command handlers first
        dispatchResult = DispatchCommandToRegisteredHandlers(command, ContinueDialogue);

        if (dispatchResult != CommandDispatchResult.NotFound) {
            // We found the command! We don't need to keep looking. (It may
            // have succeeded or failed; if it failed, it logged something
            // to the console or otherwise communicated to the developer
            // that something went wrong. Either way, we don't need to do
            // anything more here.)
            return;
        }

        // We didn't find it in the comand handlers. Try looking in the
        // game objects. If it is, continue dialogue.
        dispatchResult = DispatchCommandToGameObject(command, ContinueDialogue);

        if (dispatchResult != CommandDispatchResult.NotFound) {
            // As before: we found a handler for this command, so we stop
            // looking.
            return;
        }

        // We didn't find a method in our C# code to invoke. Try invoking on
        // the publicly exposed UnityEvent.
        //
        // We can only do this if our onCommand event is not null and would
        // do something if we invoked it, so test this now.
        //if (onCommand != null && onCommand.GetPersistentEventCount() > 0) {
        //    // We can invoke the event!
        //    onCommand.Invoke(command.Text);
        //}
        //else {
        //    // We're out of ways to handle this command! Log this as an
        //    // error.
        //    GD.PrintErr($"No Command <<{command.Text}>> was found. Did you remember to use the YarnCommand attribute or AddCommandHandler() function in C#?");
        //}
        EmitSignal("onCommand", command.Text);

        // Whether we successfully handled it via the Unity Event or not,
        // attempting to handle the command this way doesn't interrupt the
        // dialogue, so we'll continue it now.
        ContinueDialogue();
    }

    private void HandleLine(Yarn.Line line) {
        // Get the localized line from our line provider
        CurrentLine = lineProvider.GetLocalizedLine(line);

        // Expand substitutions
        var text = Yarn.Dialogue.ExpandSubstitutions(CurrentLine.RawText, CurrentLine.Substitutions);

        if (text == null) {
            GD.PrintErr($"Dialogue Runner couldn't expand substitutions in Yarn Project [{ yarnProject.Name }] node [{ CurrentNodeName }] with line ID [{ CurrentLine.TextID }]. "
                + "This usually happens because it couldn't find text in the Localization. The line may not be tagged properly. "
                + "Try re-importing this Yarn Program. "
                + "For now, Dialogue Runner will swap in CurrentLine.RawText.");
            text = CurrentLine.RawText;
        }

        // Render the markup
        CurrentLine.Text = Dialogue.ParseMarkup(text);

        CurrentLine.Status = LineStatus.Presenting;

        // Clear the set of active dialogue views, just in case
        ActiveDialogueViews.Clear();

        // the following is broken up into two stages because otherwise if the 
        // first view happens to finish first once it calls dialogue complete
        // it will empty the set of active views resulting in the line being considered
        // finished by the runner despite there being a bunch of views still waiting
        // so we do it over two loops.
        // the first finds every active view and flags it as such
        // the second then goes through them all and gives them the line

        // Mark this dialogue view as active
        foreach (var dialogueView in dialogueViews) {
            if (dialogueView == null || dialogueView.Visible == false) continue;

            ActiveDialogueViews.Add(dialogueView);
        }
        // Send line to all active dialogue views
        foreach (var dialogueView in dialogueViews) {
            if (dialogueView == null || dialogueView.Visible == false) continue;

            dialogueView.RunLine(CurrentLine,
                () => DialogueViewCompletedDelivery(dialogueView));
        }
    }
    void SelectedOption(int optionIndex) {
        if (IsOptionSelectionAllowed == false) {
            throw new InvalidOperationException("Selecting an option on the same frame that options are provided is not allowed. Wait at least one frame before selecting an option.");
        }

        // Mark that this is the currently selected option in the
        // Dialogue
        Dialogue.SetSelectedOption(optionIndex);

        if (runSelectedOptionAsLine) {
            foreach (var option in currentOptions.Options) {
                if (option.ID == optionIndex) {
                    HandleLine(option.Line);
                    return;
                }
            }

            GD.PrintErr($"Can't run selected option ({optionIndex}) as a line: couldn't find the option's associated {nameof(Yarn.Line)} object");
            ContinueDialogue();
        }
        else {
            ContinueDialogue();
        }

    }

    CommandDispatchResult DispatchCommandToRegisteredHandlers(Yarn.Command command, Action onSuccessfulDispatch) {
        return DispatchCommandToRegisteredHandlers(command.Text, onSuccessfulDispatch);
    }

    /// <inheritdoc cref="DispatchCommandToRegisteredHandlers(Command,
    /// Action)"/>
    /// <param name="command">The text of the command to
    /// dispatch.</param>
    internal CommandDispatchResult DispatchCommandToRegisteredHandlers(string command, Action onSuccessfulDispatch) {
        var commandTokens = SplitCommandText(command).ToArray();

        if (commandTokens.Length == 0) {
            // Nothing to do.
            return CommandDispatchResult.NotFound;
        }

        var firstWord = commandTokens[0];

        if (commandHandlers.ContainsKey(firstWord) == false) {
            // We don't have a registered handler for this command, but
            // some other part of the game might.
            return CommandDispatchResult.NotFound;
        }

        var @delegate = commandHandlers[firstWord];
        var methodInfo = @delegate.Method;

        object[] finalParameters;

        try {
            finalParameters = ActionManager.ParseArgs(methodInfo, commandTokens);
        }
        catch (ArgumentException e) {
            GD.PrintErr($"Can't run command {firstWord}: {e.Message}");
            return CommandDispatchResult.Failed;
        }

        if (typeof(IEnumerator).IsAssignableFrom(methodInfo.ReturnType)) {
            // This delegate returns a YieldInstruction of some kind
            // (e.g. a Coroutine). Run it, and wait for it to finish
            // before calling onSuccessfulDispatch.
            StartCoroutine(WaitForYieldInstruction(@delegate, finalParameters, onSuccessfulDispatch));
        }
        else if (typeof(void) == methodInfo.ReturnType) {
            // This method does not return anything. Invoke it and call
            // our completion handler.
            @delegate.DynamicInvoke(finalParameters);

            onSuccessfulDispatch();
        }
        else {
            GD.PrintErr($"Cannot run command {firstWord}: the provided delegate does not return a valid type (permitted return types are YieldInstruction or void)");
            return CommandDispatchResult.Failed;
        }

        return CommandDispatchResult.Success;
    }
    private static IEnumerator WaitForYieldInstruction(Delegate @theDelegate, object[] finalParametersToUse, Action onSuccessfulDispatch) {
        // Invoke the delegate.
        var yieldInstruction = @theDelegate.DynamicInvoke(finalParametersToUse);

        // Yield on the return result.
        yield return yieldInstruction;

        // Call the completion handler.
        onSuccessfulDispatch();
    }

    internal CommandDispatchResult DispatchCommandToGameObject(Yarn.Command command, Action onSuccessfulDispatch) {
        // Call out to the string version of this method, because
        // Yarn.Command's constructor is only accessible from inside
        // Yarn Spinner, but we want to be able to unit test. So, we
        // extract it, and call the underlying implementation, which is
        // testable.
        return DispatchCommandToGameObject(command.Text, onSuccessfulDispatch);
    }

    internal CommandDispatchResult DispatchCommandToGameObject(string command, System.Action onSuccessfulDispatch) {
        if (string.IsNullOrEmpty(command)) {
            throw new ArgumentException($"'{nameof(command)}' cannot be null or empty.", nameof(command));
        }

        if (onSuccessfulDispatch is null) {
            throw new ArgumentNullException(nameof(onSuccessfulDispatch));
        }


        CommandDispatchResult commandExecutionResult = ActionManager.TryExecuteCommand(SplitCommandText(command).ToArray(), out object returnValue);
        if (commandExecutionResult != CommandDispatchResult.Success) {
            return commandExecutionResult;
        }

        var enumerator = returnValue as IEnumerator;

        if (enumerator != null) {
            // Start the coroutine. When it's done, it will continue execution.
            StartCoroutine(DoYarnCommand(enumerator, onSuccessfulDispatch));
        }
        else {
            // no coroutine, so we're done!
            onSuccessfulDispatch();
        }
        return CommandDispatchResult.Success;

        IEnumerator DoYarnCommand(IEnumerator source, Action onDispatch) {
            // Wait for this command coroutine to complete
            yield return StartCoroutine(source);

            // And then signal that we're done
            onDispatch();
        }
    }

    private void PrepareForLines(IEnumerable<string> lineIDs) {
        lineProvider.PrepareForLines(lineIDs);
    }

    private void DialogueViewCompletedDelivery(DialogueViewBase dialogueView) {
        // A dialogue view just completed its delivery. Remove it from
        // the set of active views.
        ActiveDialogueViews.Remove(dialogueView);

        // Have all of the views completed? 
        if (ActiveDialogueViews.Count == 0) {
            UpdateLineStatus(CurrentLine, LineStatus.FinishedPresenting);

            // Should the line automatically become Ended as soon as
            // it's Delivered?
            if (automaticallyContinueLines) {
                // Go ahead and notify the views. 
                UpdateLineStatus(CurrentLine, LineStatus.Dismissed);

                // Additionally, tell the views to dismiss the line
                // from presentation. When each is done, it will notify
                // this dialogue runner to call
                // DialogueViewCompletedDismissal; when all have
                // finished, this dialogue runner will tell the
                // Dialogue to Continue() when all lines are done
                // dismissing the line.
                DismissLineFromViews(dialogueViews);
            }
        }
    }

    private void UpdateLineStatus(LocalizedLine line, LineStatus newStatus) {
        // Update the state of the line and let the views know.
        line.Status = newStatus;

        foreach (var dialogueView in dialogueViews) {
            if (dialogueView == null || dialogueView.Visible == false) continue;

            dialogueView.OnLineStatusChanged(line);
        }
    }

    void ContinueDialogue() {
        CurrentLine = null;
        Dialogue.Continue();
    }

    public void OnViewUserIntentNextLine() {

        if (CurrentLine == null) {
            // There's no active line, so there's nothing that can be
            // done here.
            GD.PrintErr($"{nameof(OnViewUserIntentNextLine)} was called, but no line was running.");
            return;
        }

        switch (CurrentLine.Status) {
            case LineStatus.Presenting:
                // The line has been Interrupted. Dialogue views should
                // proceed to finish the delivery of the line as
                // quickly as they can. (When all views have finished
                // their expedited delivery, they call their completion
                // handler as normal, and the line becomes Delivered.)
                UpdateLineStatus(CurrentLine, LineStatus.Interrupted);
                break;
            case LineStatus.Interrupted:
                // The line was already interrupted, and the user has
                // requested the next line again. We interpret this as
                // the user being insistent. This means the line is now
                // Ended, and the dialogue views must dismiss the line
                // immediately.
                UpdateLineStatus(CurrentLine, LineStatus.Dismissed);
                break;
            case LineStatus.FinishedPresenting:
                // The line had finished delivery (either normally or
                // because it was Interrupted), and the user has
                // indicated they want to proceed to the next line. The
                // line is therefore Ended.
                UpdateLineStatus(CurrentLine, LineStatus.Dismissed);
                break;
            case LineStatus.Dismissed:
                // The line has already been ended, so there's nothing
                // further for the views to do. (This will only happen
                // during the interval of time between a line becoming
                // Ended and the next line appearing.)
                break;
        }

        if (CurrentLine.Status == LineStatus.Dismissed) {
            // This line is Ended, so we need to tell the dialogue
            // views to dismiss it. 
            DismissLineFromViews(dialogueViews);
        }

    }

    private void DismissLineFromViews(IEnumerable<DialogueViewBase> dialogueViews) {
        ActiveDialogueViews.Clear();

        foreach (var dialogueView in dialogueViews) {
            // Skip any dialogueView that is null or not enabled
            if (dialogueView == null || dialogueView.Visible == false) {
                continue;
            }

            // we do this in two passes - first by adding each
            // dialogueView into ActiveDialogueViews, then by asking
            // them to dismiss the line - because calling
            // view.DismissLine might immediately call its completion
            // handler (which means that we'd be repeatedly returning
            // to zero active dialogue views, which means
            // DialogueViewCompletedDismissal will mark the line as
            // entirely done)
            ActiveDialogueViews.Add(dialogueView);
        }

        foreach (var dialogueView in dialogueViews) {
            if (dialogueView == null || dialogueView.Visible == false) continue;

            dialogueView.DismissLine(() => DialogueViewCompletedDismissal(dialogueView));
        }
    }

    private void DialogueViewCompletedDismissal(DialogueViewBase dialogueView) {
        // A dialogue view just completed dismissing its line. Remove
        // it from the set of active views.
        ActiveDialogueViews.Remove(dialogueView);

        // Have all of the views completed dismissal? 
        if (ActiveDialogueViews.Count == 0) {
            // Then we're ready to continue to the next piece of
            // content.
            ContinueDialogue();
        }
    }
    #endregion

    /// <summary>
    /// Splits input into a number of non-empty sub-strings, separated
    /// by whitespace, and grouping double-quoted strings into a single
    /// sub-string.
    /// </summary>
    /// <param name="input">The string to split.</param>
    /// <returns>A collection of sub-strings.</returns>
    /// <remarks>
    /// This method behaves similarly to the <see
    /// cref="string.Split(char[], StringSplitOptions)"/> method with
    /// the <see cref="StringSplitOptions"/> parameter set to <see
    /// cref="StringSplitOptions.RemoveEmptyEntries"/>, with the
    /// following differences:
    ///
    /// <list type="bullet">
    /// <item>Text that appears inside a pair of double-quote
    /// characters will not be split.</item>
    ///
    /// <item>Text that appears after a double-quote character and
    /// before the end of the input will not be split (that is, an
    /// unterminated double-quoted string will be treated as though it
    /// had been terminated at the end of the input.)</item>
    ///
    /// <item>When inside a pair of double-quote characters, the string
    /// <c>\\</c> will be converted to <c>\</c>, and the string
    /// <c>\"</c> will be converted to <c>"</c>.</item>
    /// </list>
    /// </remarks>
    public static IEnumerable<string> SplitCommandText(string input) {
        var reader = new System.IO.StringReader(input.Normalize());

        int c;

        var results = new List<string>();
        var currentComponent = new System.Text.StringBuilder();

        while ((c = reader.Read()) != -1) {
            if (char.IsWhiteSpace((char)c)) {
                if (currentComponent.Length > 0) {
                    // We've reached the end of a run of visible
                    // characters. Add this run to the result list and
                    // prepare for the next one.
                    results.Add(currentComponent.ToString());
                    currentComponent.Clear();
                }
                else {
                    // We encountered a whitespace character, but
                    // didn't have any characters queued up. Skip this
                    // character.
                }

                continue;
            }
            else if (c == '\"') {
                // We've entered a quoted string!
                while (true) {
                    c = reader.Read();
                    if (c == -1) {
                        // Oops, we ended the input while parsing a
                        // quoted string! Dump our current word
                        // immediately and return.
                        results.Add(currentComponent.ToString());
                        return results;
                    }
                    else if (c == '\\') {
                        // Possibly an escaped character!
                        var next = reader.Peek();
                        if (next == '\\' || next == '\"') {
                            // It is! Skip the \ and use the character after it.
                            reader.Read();
                            currentComponent.Append((char)next);
                        }
                        else {
                            // Oops, an invalid escape. Add the \ and
                            // whatever is after it.
                            currentComponent.Append((char)c);
                        }
                    }
                    else if (c == '\"') {
                        // The end of a string!
                        break;
                    }
                    else {
                        // Any other character. Add it to the buffer.
                        currentComponent.Append((char)c);
                    }
                }

                results.Add(currentComponent.ToString());
                currentComponent.Clear();
            }
            else {
                currentComponent.Append((char)c);
            }
        }

        if (currentComponent.Length > 0) {
            results.Add(currentComponent.ToString());
        }

        return results;
    }
}
