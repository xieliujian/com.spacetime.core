# LaunchModule Startup Skeleton Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a reusable package-level `GameLauncher → GameApplication → LaunchModule` startup path with one concrete launch-node class per file.

**Architecture:** All code lives in `com.spacetime.core` so a new game can mount `GameLauncher` and run the default empty startup tree immediately. `LaunchModule` uses an explicit `LaunchNodeType` factory and composes sequential and parallel nodes; real game services can be injected later without changing the initial skeleton.

**Tech Stack:** Unity 2022.3.35f1c1, C#, `ST.Core.IManager`, `ST.Core.Logging.Logger`, NUnit Unity EditMode tests.

---

## File responsibility map

| Path | Single responsibility |
|---|---|
| `Runtime/Scripts/Launch/LaunchNodeState.cs` | Shared lifecycle state enum. |
| `Runtime/Scripts/Launch/LaunchNodeType.cs` | Exact concrete launch-node identifiers. |
| `Runtime/Scripts/Launch/Nodes/LaunchNode.cs` | Base state machine and exception boundary. |
| `Runtime/Scripts/Launch/Nodes/SequenceNode.cs` | Ordered child scheduling. |
| `Runtime/Scripts/Launch/Nodes/ParallelNode.cs` | Concurrent child scheduling. |
| `Runtime/Scripts/Launch/Nodes/*Node.cs` | One concrete empty startup action per file. |
| `Runtime/Scripts/Launch/LaunchModule.cs` | Node factory, standard tree construction, root driving. |
| `Runtime/Scripts/Application/GameApplication.cs` | Own and drive one `LaunchModule`. |
| `Runtime/Scripts/Application/GameLauncher.cs` | Forward Unity scene lifecycle to `GameApplication`. |
| `Tests/Editor/ProbeLaunchNode.cs` | Controllable test fake, isolated in its own file. |
| `Tests/Editor/LaunchNodeTest.cs` | Base-node state transition tests. |
| `Tests/Editor/SequenceNodeTest.cs` | Sequential scheduling tests. |
| `Tests/Editor/ParallelNodeTest.cs` | Parallel scheduling tests. |
| `Tests/Editor/LaunchModuleTest.cs` | Factory mapping and exact tree tests. |
| `Tests/Editor/GameApplicationTest.cs` | Application lifecycle tests. |

All runtime code uses namespace `ST.Core.Launch`. Tests use `ST.Core.Launch.Tests`. Every new
directory and file receives its exact same path plus `.meta`; directory metas are
`Runtime/Scripts/Application.meta`, `Runtime/Scripts/Launch.meta`,
`Runtime/Scripts/Launch/Nodes.meta`, `Tests.meta`, and `Tests/Editor.meta`.

## Exact verification command template

Before each Unity run, create a result directory outside the Unity project:

```powershell
New-Item -ItemType Directory -Force -Path 'C:\Users\xieliujian\AppData\Local\Temp\codex-launch-tests'
```

Focused EditMode command, replacing `<FILTER>` and `<NAME>` literally in each task:

```powershell
& 'C:\Program Files\Unity\2022.3.35f1c1\Editor\Unity.exe' -batchmode -nographics -quit -projectPath 'D:\xieliujian\com.spacetime.core' -runTests -testPlatform EditMode -testFilter '<FILTER>' -testResults 'C:\Users\xieliujian\AppData\Local\Temp\codex-launch-tests\<NAME>.xml' -logFile 'C:\Users\xieliujian\AppData\Local\Temp\codex-launch-tests\<NAME>.log'
```

For RED, exit must be nonzero and the log must contain the expected missing type or failing assertion.
For GREEN, exit must be zero and the XML must contain `result="Passed"` and `failed="0"`.

---

## Chunk 1: Core node state machines

### Task 1: LaunchNode lifecycle

**Files:**
- Create: `Tests/Editor/com.spacetime.core.tests.asmdef`
- Create: `Tests/Editor/ProbeLaunchNode.cs`
- Create: `Tests/Editor/LaunchNodeTest.cs`
- Create: `Runtime/Scripts/Launch/LaunchNodeState.cs`
- Create: `Runtime/Scripts/Launch/Nodes/LaunchNode.cs`

- [ ] **Step 1: Create the test assembly and failing lifecycle tests**

  The asmdef is exactly:

  ```json
  {
      "name": "com.spacetime.core.tests",
      "rootNamespace": "",
      "references": ["com.spacetime.core.runtime"],
      "includePlatforms": ["Editor"],
      "excludePlatforms": [],
      "allowUnsafeCode": false,
      "overrideReferences": false,
      "precompiledReferences": [],
      "autoReferenced": false,
      "defineConstraints": [],
      "versionDefines": [],
      "noEngineReferences": false,
      "optionalUnityReferences": ["TestAssemblies"]
  }
  ```

  `ProbeLaunchNode` is a separate public sealed test class. It exposes counters
  `startCount/updateCount/stopCount` and flags `completeOnStart`, `failOnStart`,
  `completeOnUpdate`, `failOnUpdate`, `throwOnStart`, `throwOnUpdate`,
  `throwOnStop`. Its overrides increment counters, then throw, call `Fail("Probe failure.")`,
  or call `Complete()` according to the flags.

  `LaunchNodeTest` contains independent NUnit tests for constructor validation, initial state,
  ignored pre-start update, idempotent start, separate start/update exception conversion,
  null/empty failure normalization, ready/running stop behavior, stop exception containment, and
  terminal-state idempotence.

- [ ] **Step 2: Verify the lifecycle tests are RED**

  Run the exact command template with filter `ST.Core.Launch.Tests.LaunchNodeTest` and name
  `launch-node-red`. Expected: nonzero exit and `CS0234` or `CS0246` for missing
  `ST.Core.Launch.LaunchNode`.

- [ ] **Step 3: Implement LaunchNodeState and LaunchNode**

  `LaunchNodeState` contains exactly `Ready`, `Running`, `Succeeded`, `Failed`,
  `Stopped`.

  `LaunchNode` is public abstract. Its constructor validates a non-empty name. Public read-only
  properties are `name`, `state`, `error`, `isDone`, and `isSucceeded`. Public methods
  are `Start()`, `DoUpdate()`, and `Stop()`.

  Exact transition rules:

  - `Start()` only acts from `Ready`: set `Running`, call `OnStart()`, catch
    `Exception` and fail with `exception.ToString()`.
  - `DoUpdate()` only acts from `Running`: call `OnUpdate()`, catch and fail identically.
  - `Stop()` from `Ready` directly sets `Stopped`; from `Running` calls `OnStop()` in
    try/catch, logs caught exceptions through `Logger.LogException`, then always sets `Stopped`.
  - `Complete()` and `Fail()` act only from `Running`.
  - `Fail()` stores `"Launch node failed."` when input is null or empty.
  - `isDone` is true for `Succeeded`, `Failed`, or `Stopped`.

  Protected hooks are `abstract void OnStart()`, virtual empty `OnUpdate()`, virtual empty
  `OnStop()`, plus `Complete()` and `Fail(string error)`.

- [ ] **Step 4: Normalize Task 1 C# sources to UTF-8 BOM**

  Rewrite only `ProbeLaunchNode.cs`, `LaunchNodeTest.cs`, `LaunchNodeState.cs`, and
  `LaunchNode.cs` using `UTF8Encoding(true)`; verify their first three bytes are `EF BB BF`.

- [ ] **Step 5: Verify the lifecycle tests are GREEN**

  Run the exact command template with filter `ST.Core.Launch.Tests.LaunchNodeTest` and name
  `launch-node-green`. Expected: exit 0, XML `result="Passed"`, `failed="0"`.

- [ ] **Step 6: Commit the lifecycle unit**

  Stage all Task 1 files including sibling directory metas, run `git diff --cached --check`, then:

  ```powershell
  git add -- Runtime/Scripts/Launch Runtime/Scripts/Launch.meta Tests Tests.meta
  git diff --cached --check
  git commit -m "feat: add launch node lifecycle"
  ```

### Task 2: SequenceNode scheduling

**Files:**
- Create: `Tests/Editor/SequenceNodeTest.cs`
- Create: `Runtime/Scripts/Launch/Nodes/SequenceNode.cs`

- [ ] **Step 1: Write failing sequence tests**

  Tests cover empty synchronous success, exact append order, synchronous advancement, waiting for
  a running child, asynchronous failure preserving the first exact error, prevention of later
  starts, active-child stop propagation, null rejection, and append-after-start rejection.

- [ ] **Step 2: Verify sequence tests are RED**

  Run the exact command template with filter `ST.Core.Launch.Tests.SequenceNodeTest` and name
  `sequence-node-red`. Expected: nonzero exit with missing `SequenceNode`.

- [ ] **Step 3: Implement SequenceNode**

  Public sealed `SequenceNode : LaunchNode` owns one preallocated `List<LaunchNode>` and an
  integer current index. `Append` returns void, rejects null, and requires `Ready`.
  `OnStart()` and `OnUpdate()` call one private `Advance()` method. `Advance()` starts the
  current ready child, returns on running, fails with the child's unchanged error on failed/stopped,
  advances on success, and completes after the last child. `OnStop()` stops the current child only
  if it is running.

- [ ] **Step 4: Normalize and verify SequenceNode**

  Add UTF-8 BOM to both new C# files. Run the exact command template with filter
  `ST.Core.Launch.Tests.SequenceNodeTest` and name `sequence-node-green`.
  Expected: exit 0, XML passed, zero failures.

- [ ] **Step 5: Commit the sequence unit**

  Run `git add -- Runtime/Scripts/Launch Tests`, then `git diff --cached --check`, then commit
  with message `feat: add sequential launch scheduling`.

### Task 3: ParallelNode scheduling

**Files:**
- Create: `Tests/Editor/ParallelNodeTest.cs`
- Create: `Runtime/Scripts/Launch/Nodes/ParallelNode.cs`

- [ ] **Step 1: Write failing parallel tests**

  Tests cover empty synchronous success, exact start order, waiting for all children, asynchronous
  failure preserving the first exact error and stopping siblings, synchronous failure preventing
  later starts and stopping earlier running children, parent-stop propagation, null rejection, and
  append-after-start rejection.

- [ ] **Step 2: Verify parallel tests are RED**

  Run the exact command template with filter `ST.Core.Launch.Tests.ParallelNodeTest` and name
  `parallel-node-red`. Expected: nonzero exit with missing `ParallelNode`.

- [ ] **Step 3: Implement ParallelNode**

  Public sealed `ParallelNode : LaunchNode` owns one preallocated `List<LaunchNode>`.
  `Append` has the same validation as `SequenceNode`. `OnStart()` starts children in order;
  after each start it immediately checks failure/stopped, stops all earlier running children, and
  fails without starting later children. `OnUpdate()` updates running children, then scans for
  the first failed/stopped child; on failure it stops every other running child and preserves that
  child's error. It completes only when every child succeeded. Empty children complete in start.
  `OnStop()` stops every running child.

- [ ] **Step 4: Normalize and verify ParallelNode**

  Add UTF-8 BOM to both new C# files. Run the exact command template with filter
  `ST.Core.Launch.Tests.ParallelNodeTest` and name `parallel-node-green`.
  Expected: exit 0, XML passed, zero failures.

- [ ] **Step 5: Commit the parallel unit**

  Run `git add -- Runtime/Scripts/Launch Tests`, then `git diff --cached --check`, then commit
  with message `feat: add parallel launch scheduling`.

---

## Chunk 2: Concrete nodes and LaunchModule

### Task 4: Add bootstrap concrete nodes

**Files:**
- Create: `Tests/Editor/BootstrapLaunchNodeTest.cs`
- Create: `Runtime/Scripts/Launch/LaunchNodeType.cs`
- Create: `Runtime/Scripts/Launch/Nodes/WaitFrameNode.cs`
- Create: `Runtime/Scripts/Launch/Nodes/AppInitNode.cs`
- Create: `Runtime/Scripts/Launch/Nodes/OpenLaunchShellNode.cs`
- Create: `Runtime/Scripts/Launch/Nodes/SplashGlobalResLoadNode.cs`
- Create: `Runtime/Scripts/Launch/Nodes/DownloadServerConfigNode.cs`
- Create: `Runtime/Scripts/Launch/Nodes/TableLoadNode.cs`

- [ ] **Step 1: Write failing bootstrap-node tests**

  Test each class can be constructed, its `name` exactly matches the class name without the
  `Node` suffix, and it succeeds immediately after `Start()`. Test `WaitFrameNode` separately:
  it is running after `Start()`, remains uncompleted without an update, and succeeds after exactly
  one `DoUpdate()`. Assert the enum starts with `None` and contains the exact members listed in
  Task 5 before ending with `Max`.

- [ ] **Step 2: Verify bootstrap-node tests are RED**

  Run the exact command template with filter `ST.Core.Launch.Tests.BootstrapLaunchNodeTest` and
  name `bootstrap-nodes-red`. Expected: nonzero exit with missing concrete node types.

- [ ] **Step 3: Implement the enum and bootstrap nodes**

  `LaunchNodeType` has this exact order:

  ```text
  None, WaitFrame, AppInit, OpenLaunchShell, SplashGlobalResLoad,
  DownloadServerConfig, TableLoad, CheckDownloadServerConfigSuccess,
  BeforeLoginGlobalResLoad, DownloadServerDynamic,
  CheckDownloadServerDynamicSuccess, SDKInit, SDKInitCheck, CheckVersion,
  Update, ReloadRes, CompileShader, CheckSubpackUpdate, LaunchGC,
  OpenLoginPanel, SDKLogin, End, Max
  ```

  `WaitFrameNode.OnStart()` intentionally does nothing; its `OnUpdate()` calls `Complete()`.
  Every other Task 4 node has a public parameterless constructor calling the base with its exact
  stable name and an `OnStart()` containing only `Complete()`. Each class is public sealed and
  is the only class in its file.

- [ ] **Step 4: Normalize and verify bootstrap nodes**

  Add UTF-8 BOM to all Task 4 C# files. Run the exact command template with filter
  `ST.Core.Launch.Tests.BootstrapLaunchNodeTest` and name `bootstrap-nodes-green`.
  Expected: exit 0, XML passed, zero failures.

- [ ] **Step 5: Commit bootstrap nodes**

  Run `git add -- Runtime/Scripts/Launch Tests`, then `git diff --cached --check`, then commit
  Task 4 sources and metas with message
  `feat: add bootstrap launch nodes`.

### Task 5: Add remaining concrete nodes

**Files:**
- Create: `Tests/Editor/RemainingLaunchNodeTest.cs`
- Create: `Runtime/Scripts/Launch/Nodes/CheckDownloadServerConfigSuccessNode.cs`
- Create: `Runtime/Scripts/Launch/Nodes/BeforeLoginGlobalResLoadNode.cs`
- Create: `Runtime/Scripts/Launch/Nodes/DownloadServerDynamicNode.cs`
- Create: `Runtime/Scripts/Launch/Nodes/CheckDownloadServerDynamicSuccessNode.cs`
- Create: `Runtime/Scripts/Launch/Nodes/SDKInitNode.cs`
- Create: `Runtime/Scripts/Launch/Nodes/SDKInitCheckNode.cs`
- Create: `Runtime/Scripts/Launch/Nodes/CheckVersionNode.cs`
- Create: `Runtime/Scripts/Launch/Nodes/UpdateNode.cs`
- Create: `Runtime/Scripts/Launch/Nodes/ReloadResNode.cs`
- Create: `Runtime/Scripts/Launch/Nodes/CompileShaderNode.cs`
- Create: `Runtime/Scripts/Launch/Nodes/CheckSubpackUpdateNode.cs`
- Create: `Runtime/Scripts/Launch/Nodes/LaunchGCNode.cs`
- Create: `Runtime/Scripts/Launch/Nodes/OpenLoginPanelNode.cs`
- Create: `Runtime/Scripts/Launch/Nodes/SDKLoginNode.cs`
- Create: `Runtime/Scripts/Launch/Nodes/EndNode.cs`

- [ ] **Step 1: Write failing remaining-node tests**

  Use one NUnit string-based test case per concrete class. Resolve the class by name through
  `typeof(LaunchNode).Assembly.GetType(...)`, so all tests compile before any of the 15 concrete
  types exist. Each test asserts that the type exists, constructs it, verifies the exact stable
  name shown below, starts it, and asserts `Succeeded`:

  ```text
  CheckDownloadServerConfigSuccessNode -> CheckDownloadServerConfigSuccess
  BeforeLoginGlobalResLoadNode         -> BeforeLoginGlobalResLoad
  DownloadServerDynamicNode            -> DownloadServerDynamic
  CheckDownloadServerDynamicSuccessNode-> CheckDownloadServerDynamicSuccess
  SDKInitNode                           -> SDKInit
  SDKInitCheckNode                      -> SDKInitCheck
  CheckVersionNode                      -> CheckVersion
  UpdateNode                            -> Update
  ReloadResNode                         -> ReloadRes
  CompileShaderNode                     -> CompileShader
  CheckSubpackUpdateNode                -> CheckSubpackUpdate
  LaunchGCNode                          -> LaunchGC
  OpenLoginPanelNode                    -> OpenLoginPanel
  SDKLoginNode                          -> SDKLogin
  EndNode                               -> End
  ```

- [ ] **Step 2: Verify remaining-node tests are RED**

  Run the exact command template with filter `ST.Core.Launch.Tests.RemainingLaunchNodeTest` and
  name `remaining-nodes-red`. Expected: nonzero exit because all 15 reflected types are absent;
  the test assembly itself must compile successfully.

- [ ] **Step 3: Add CheckDownloadServerConfigSuccessNode.cs**

  Create only `CheckDownloadServerConfigSuccessNode`, using stable name
  `CheckDownloadServerConfigSuccess`, and rerun the class. Its string-based test case must pass;
  failures for types not created yet are expected.

- [ ] **Step 4: Add BeforeLoginGlobalResLoadNode.cs**

  Create only `BeforeLoginGlobalResLoadNode`, using stable name `BeforeLoginGlobalResLoad`,
  and rerun the class. This new case must pass while later missing-type cases remain red.

- [ ] **Step 5: Add DownloadServerDynamicNode.cs**

  Create only `DownloadServerDynamicNode`, using stable name `DownloadServerDynamic`, and run
  its single test case.

- [ ] **Step 6: Add CheckDownloadServerDynamicSuccessNode.cs**

  Create only `CheckDownloadServerDynamicSuccessNode`, using stable name
  `CheckDownloadServerDynamicSuccess`, and run its single test case.

- [ ] **Step 7: Add SDKInitNode.cs**

  Create only `SDKInitNode`, using stable name `SDKInit`, and run its single test case.

- [ ] **Step 8: Add SDKInitCheckNode.cs**

  Create only `SDKInitCheckNode`, using stable name `SDKInitCheck`, and run its single test case.

- [ ] **Step 9: Add CheckVersionNode.cs**

  Create only `CheckVersionNode`, using stable name `CheckVersion`, and run its single test case.

- [ ] **Step 10: Add UpdateNode.cs**

  Create only `UpdateNode`, using stable name `Update`, and run its single test case.

- [ ] **Step 11: Add ReloadResNode.cs**

  Create only `ReloadResNode`, using stable name `ReloadRes`, and run its single test case.

- [ ] **Step 12: Add CompileShaderNode.cs**

  Create only `CompileShaderNode`, using stable name `CompileShader`, and run its single test case.

- [ ] **Step 13: Add CheckSubpackUpdateNode.cs**

  Create only `CheckSubpackUpdateNode`, using stable name `CheckSubpackUpdate`, and run its
  single test case.

- [ ] **Step 14: Add LaunchGCNode.cs**

  Create only `LaunchGCNode`, using stable name `LaunchGC`, and run its single test case.

- [ ] **Step 15: Add OpenLoginPanelNode.cs**

  Create only `OpenLoginPanelNode`, using stable name `OpenLoginPanel`, and run its single test.

- [ ] **Step 16: Add SDKLoginNode.cs**

  Create only `SDKLoginNode`, using stable name `SDKLogin`, and run its single test case.

- [ ] **Step 17: Add EndNode.cs**

  Create only `EndNode`, using stable name `End`, and run its single test case.

- [ ] **Step 18: Normalize and verify remaining nodes**

  Add UTF-8 BOM to all Task 5 C# files. Run the exact command template with filter
  `ST.Core.Launch.Tests.RemainingLaunchNodeTest` and name `remaining-nodes-green`.
  Expected: exit 0, XML passed, zero failures.

- [ ] **Step 19: Verify one concrete node per file**

  Run:

  ```powershell
  rg -n '^\s*public sealed class .*Node' Runtime/Scripts/Launch/Nodes
  ```

  Expected: one declaration in each concrete node file and no file containing two declarations.
  Compare basenames mechanically with class names; `LaunchNode.cs`, `SequenceNode.cs`, and
  `ParallelNode.cs` are the only intentional abstract/composite exceptions.

- [ ] **Step 20: Commit remaining nodes**

  Run `git add -- Runtime/Scripts/Launch Tests`, then `git diff --cached --check`, then commit
  Task 5 sources and metas with message
  `feat: add standard launch action nodes`.

### Task 6: Build and drive the LaunchModule tree

**Files:**
- Create: `Tests/Editor/LaunchModuleTest.cs`
- Create: `Runtime/Scripts/Launch/LaunchModule.cs`

- [ ] **Step 1: Write failing factory and tree tests**

  Invoke private `CreateNode` only through reflection in tests. For every enum value between
  `None` and `Max`, assert the returned runtime type equals `<EnumMember>Node`. Assert
  `None`, `Max`, and `(LaunchNodeType)int.MaxValue` each wrap an
  `ArgumentOutOfRangeException`.

  After `Launch()`, reflect `m_RootNode` and the composite child lists. Assert exact root name
  `LaunchNode`, exact splash name `Splash`, exact root order:

  ```text
  WaitFrameNode, AppInitNode, ParallelNode,
  CheckDownloadServerConfigSuccessNode, BeforeLoginGlobalResLoadNode,
  DownloadServerDynamicNode, CheckDownloadServerDynamicSuccessNode,
  SDKInitNode, SDKInitCheckNode, CheckVersionNode, UpdateNode, ReloadResNode,
  CompileShaderNode, CheckSubpackUpdateNode, LaunchGCNode, OpenLoginPanelNode,
  SDKLoginNode, EndNode
  ```

  Assert splash order:

  ```text
  OpenLaunchShellNode, SplashGlobalResLoadNode, DownloadServerConfigNode, TableLoadNode
  ```

  Add lifecycle tests: initial `Ready` with empty non-null error; update before launch ignored;
  launch enters `Running`; repeated launch retains the same root; one update reaches
  `Succeeded`; post-completion update is ignored; close from ready/running yields `Stopped`;
  close after success preserves `Succeeded`; repeated close is safe.

- [ ] **Step 2: Verify LaunchModule tests are RED**

  Run the exact command template with filter `ST.Core.Launch.Tests.LaunchModuleTest` and name
  `launch-module-red`. Expected: nonzero exit with missing `LaunchModule`.

- [ ] **Step 3: Implement LaunchModule**

  Public sealed `LaunchModule : IManager` owns `LaunchNode m_RootNode`,
  `LaunchNodeState m_State`, non-null `string m_Error`, and `bool m_IsFailureLogged`.
  Public read-only properties are `state`, `error`, `isDone`, and `isSucceeded`.
  `DoInit()` and `DoLateUpdate()` are documented no-ops.

  `Launch()` acts only from `Ready`, constructs the exact tree from Step 1, sets `Running`,
  starts the root, and synchronizes any synchronous terminal state. It catches construction/start
  exceptions, stores `Exception.ToString()`, sets `Failed`, and calls
  `Logger.LogException(exception)`. The shared state-synchronization method logs a root failure
  once with non-variadic `Logger.LogError(m_Error)` regardless of whether synchronization is
  reached from `Launch()` or `DoUpdate()`.

  `DoUpdate()` acts only in `Running`, drives the root, synchronizes state, and logs a root
  `DoClose()` changes ready/running to stopped, stopping a running root; it preserves
  succeeded/failed/stopped states.

  Private `CreateNode(LaunchNodeType type)` is an exhaustive switch matching Task 4's enum.
  Invalid/sentinel values throw `ArgumentOutOfRangeException`.

- [ ] **Step 4: Normalize and verify LaunchModule**

  Add UTF-8 BOM to both new C# files. Run the exact command template with filter
  `ST.Core.Launch.Tests.LaunchModuleTest` and name `launch-module-green`.
  Expected: exit 0, XML passed, zero failures.

- [ ] **Step 5: Verify enum, factory, class, and file coverage**

  Run:

  ```powershell
  rg -n '^\s*(None|WaitFrame|AppInit|OpenLaunchShell|SplashGlobalResLoad|DownloadServerConfig|TableLoad|CheckDownloadServerConfigSuccess|BeforeLoginGlobalResLoad|DownloadServerDynamic|CheckDownloadServerDynamicSuccess|SDKInit|SDKInitCheck|CheckVersion|Update|ReloadRes|CompileShader|CheckSubpackUpdate|LaunchGC|OpenLoginPanel|SDKLogin|End|Max),?$' Runtime/Scripts/Launch/LaunchNodeType.cs
  rg -n 'case LaunchNodeType\.' Runtime/Scripts/Launch/LaunchModule.cs
  rg -n '^\s*public sealed class .*Node' Runtime/Scripts/Launch/Nodes
  ```

  Expected: 23 enum entries including sentinels, 21 factory cases, and one matching concrete class
  file for each of the 21 non-sentinel entries.

- [ ] **Step 6: Commit LaunchModule**

  Run `git add -- Runtime/Scripts/Launch Tests`, then `git diff --cached --check`, then commit
  Task 6 sources and metas with message
  `feat: add launch module node tree`.

---

## Chunk 3: Application entry and final verification

### Task 7: GameApplication lifecycle

**Files:**
- Create: `Tests/Editor/GameApplicationTest.cs`
- Create: `Runtime/Scripts/Application/GameApplication.cs`

- [ ] **Step 1: Write failing GameApplication tests**

  Tests prove: `launchModule` is null before initialization; pre-init frame calls are safe;
  `DoInit()` creates and launches a running module; repeated `DoInit()` preserves the same
  instance; one `DoUpdate()` completes the default tree; close while running stops the module;
  repeated close is safe; update and late-update after close preserve state; close after successful
  completion preserves success.

- [ ] **Step 2: Verify GameApplication tests are RED**

  Run the exact command template with filter `ST.Core.Launch.Tests.GameApplicationTest` and name
  `game-application-red`. Expected: nonzero exit with missing `GameApplication`.

- [ ] **Step 3: Implement GameApplication**

  Public sealed `GameApplication : IManager` owns `LaunchModule m_LaunchModule`,
  `bool m_IsInitialized`, and `bool m_IsClosed`. Public read-only property `launchModule`
  returns null before initialization.

  `DoInit()` returns immediately when initialized or closed, then constructs one module, calls
  `DoInit()`, calls `Launch()`, and marks initialized. `DoUpdate()` and `DoLateUpdate()`
  forward only while initialized and not closed. `DoClose()` is idempotent, marks closed, delegates
  close when a module exists, and preserves the module reference.

- [ ] **Step 4: Normalize and verify GameApplication**

  Add UTF-8 BOM to both new C# files. Run the exact command template with filter
  `ST.Core.Launch.Tests.GameApplicationTest` and name `game-application-green`.
  Expected: exit 0, XML passed, zero failures.

- [ ] **Step 5: Commit GameApplication**

  Run `git add -- Runtime/Scripts/Application Runtime/Scripts/Application.meta Tests`, then
  `git diff --cached --check`, then commit Task 7 sources and metas with message
  `feat: add game application startup coordinator`.

### Task 8: Scene-mounted GameLauncher

**Files:**
- Create: `Runtime/Scripts/Application/GameLauncher.cs`

- [ ] **Step 1: Add a compile-time GameLauncher contract test**

  Extend `GameApplicationTest.cs` with reflection assertions that `GameLauncher` is public,
  sealed, derives from `MonoBehaviour`, and declares non-public instance methods named
  `Awake`, `Update`, `LateUpdate`, and `OnDestroy`.

- [ ] **Step 2: Verify the GameLauncher test is RED**

  Run the exact command template with filter `ST.Core.Launch.Tests.GameApplicationTest` and name
  `game-launcher-red`. Expected: nonzero exit with missing `GameLauncher`.

- [ ] **Step 3: Implement GameLauncher**

  Public sealed `GameLauncher : MonoBehaviour` owns `GameApplication m_GameApplication`.
  `Awake()` constructs and initializes it. `Update()`, `LateUpdate()`, and `OnDestroy()`
  use the project's two-line null guard and forward to the matching application lifecycle method.
  `OnDestroy()` then clears the reference. The class has no singleton, serialized fields,
  automatic object creation, service dependencies, or business logic.

- [ ] **Step 4: Normalize and verify GameLauncher**

  Add UTF-8 BOM to `GameLauncher.cs` and the modified test. Run the exact command template with
  filter `ST.Core.Launch.Tests.GameApplicationTest` and name `game-launcher-green`.
  Expected: exit 0, XML passed, zero failures.

- [ ] **Step 5: Commit GameLauncher**

  Run `git add -- Runtime/Scripts/Application Tests`, then `git diff --cached --check`, then
  commit Task 8 sources and metas with message
  `feat: add scene game launcher`.

### Task 9: Full verification and review

**Files:**
- Review: every file created by Tasks 1–8
- Track: generated meta files only; test result files live outside the Unity project

- [ ] **Step 1: Run all new EditMode tests**

  Run:

  ```powershell
  New-Item -ItemType Directory -Force -Path 'C:\Users\xieliujian\AppData\Local\Temp\codex-launch-tests'
  & 'C:\Program Files\Unity\2022.3.35f1c1\Editor\Unity.exe' -batchmode -nographics -quit -projectPath 'D:\xieliujian\com.spacetime.core' -runTests -testPlatform EditMode -testFilter 'ST.Core.Launch.Tests' -testResults 'C:\Users\xieliujian\AppData\Local\Temp\codex-launch-tests\launch-final.xml' -logFile 'C:\Users\xieliujian\AppData\Local\Temp\codex-launch-tests\launch-final.log'
  ```

  Expected: process exit 0; result XML reports `result="Passed"`, `failed="0"`; log has no
  compilation error.

- [ ] **Step 2: Run a fresh Unity compilation**

  Run:

  ```powershell
  & 'C:\Program Files\Unity\2022.3.35f1c1\Editor\Unity.exe' -batchmode -nographics -quit -projectPath 'D:\xieliujian\com.spacetime.core' -logFile 'C:\Users\xieliujian\AppData\Local\Temp\codex-launch-tests\launch-compile.log'
  ```

  Expected: exit 0 and no `error CS` or `Scripts have compiler errors` in the fresh log.

- [ ] **Step 3: Run mechanical structure checks**

  Run:

  ```powershell
  rg -n '^\s*public sealed class .*Node' Runtime/Scripts/Launch/Nodes
  rg -n 'case LaunchNodeType\.' Runtime/Scripts/Launch/LaunchModule.cs
  git diff --check a34d35c..HEAD
  git status --short
  ```

  Expected: 23 sealed node declarations including two composites, 21 factory cases, no whitespace
  errors, and no unrelated modified files.

- [ ] **Step 4: Verify all C# BOMs and Unity metas**

  Enumerate only new C# files from this plan. For each, read the first three bytes and require
  `EF BB BF`. For every new file and directory, require its exact sibling meta path. Fail the
  check on the first missing BOM or meta; do not modify unrelated files.

- [ ] **Step 5: Perform LingRen and GC compliance audit**

  Check all touched C# files for: four spaces, `m_` fields, lower-camel public properties,
  PascalCase classes/methods, omitted unnecessary `private`, Chinese meaningful XML docs on every
  class/member, two-line null guards, no `?.`, no LINQ, no direct `UnityEngine.Debug`, no
  three-part string concatenation, no collection allocation or variadic log calls in `DoUpdate()`,
  and practical 120-character lines.

- [ ] **Step 6: Dispatch final code review and fix every blocking issue**

  Review the complete diff against
  `.agents/superpowers/specs/2026-09-07-launch-module-design.md` and this plan. Re-run Steps 1–5 after
  any fix.

- [ ] **Step 7: Report the direct-main result**

  List the created commits, test count and result, Unity compilation result, final file locations,
  and any verification limitation. The user explicitly authorized direct development on `main`;
  do not create or merge another branch.

---

## Chunk 4: Exact core implementation listings

The following listings are the exact production contents required by Tasks 1–3.

### LaunchNodeState.cs

```csharp
namespace ST.Core.Launch
{
    /// <summary>
    /// 启动节点生命周期状态。
    /// </summary>
    public enum LaunchNodeState : byte
    {
        /// <summary>尚未启动。</summary>
        Ready,
        /// <summary>正在执行。</summary>
        Running,
        /// <summary>成功完成。</summary>
        Succeeded,
        /// <summary>执行失败。</summary>
        Failed,
        /// <summary>已被停止。</summary>
        Stopped,
    }
}
```

---

## Chunk 8: Exact launch and application test listings

### BootstrapLaunchNodeTest.cs

```csharp
using System;
using NUnit.Framework;

namespace ST.Core.Launch.Tests
{
    /// <summary>
    /// 首批具体启动节点测试。
    /// </summary>
    public sealed class BootstrapLaunchNodeTest
    {
        /// <summary>验证立即完成的首批节点。</summary>
        [TestCase(typeof(AppInitNode), "AppInit")]
        [TestCase(typeof(OpenLaunchShellNode), "OpenLaunchShell")]
        [TestCase(typeof(SplashGlobalResLoadNode), "SplashGlobalResLoad")]
        [TestCase(typeof(DownloadServerConfigNode), "DownloadServerConfig")]
        [TestCase(typeof(TableLoadNode), "TableLoad")]
        public void ImmediateNodeSucceeds(Type nodeType, string expectedName)
        {
            LaunchNode node = (LaunchNode)Activator.CreateInstance(nodeType);
            node.Start();

            Assert.AreEqual(expectedName, node.name);
            Assert.IsTrue(node.isSucceeded);
        }

        /// <summary>验证等待帧节点在首个更新完成。</summary>
        [Test]
        public void WaitFrameCompletesOnFirstUpdate()
        {
            WaitFrameNode node = new WaitFrameNode();
            node.Start();
            Assert.AreEqual(LaunchNodeState.Running, node.state);
            node.DoUpdate();
            Assert.IsTrue(node.isSucceeded);
        }

        /// <summary>验证节点枚举顺序。</summary>
        [Test]
        public void NodeTypeOrderIsStable()
        {
            LaunchNodeType[] expected =
            {
                LaunchNodeType.None, LaunchNodeType.WaitFrame, LaunchNodeType.AppInit,
                LaunchNodeType.OpenLaunchShell, LaunchNodeType.SplashGlobalResLoad,
                LaunchNodeType.DownloadServerConfig, LaunchNodeType.TableLoad,
                LaunchNodeType.CheckDownloadServerConfigSuccess,
                LaunchNodeType.BeforeLoginGlobalResLoad, LaunchNodeType.DownloadServerDynamic,
                LaunchNodeType.CheckDownloadServerDynamicSuccess, LaunchNodeType.SDKInit,
                LaunchNodeType.SDKInitCheck, LaunchNodeType.CheckVersion, LaunchNodeType.Update,
                LaunchNodeType.ReloadRes, LaunchNodeType.CompileShader,
                LaunchNodeType.CheckSubpackUpdate, LaunchNodeType.LaunchGC,
                LaunchNodeType.OpenLoginPanel, LaunchNodeType.SDKLogin, LaunchNodeType.End,
                LaunchNodeType.Max,
            };

            CollectionAssert.AreEqual(expected, Enum.GetValues(typeof(LaunchNodeType)));
        }
    }
}
```

### RemainingLaunchNodeTest.cs

```csharp
using System;
using NUnit.Framework;

namespace ST.Core.Launch.Tests
{
    /// <summary>
    /// 后续具体启动节点测试。
    /// </summary>
    public sealed class RemainingLaunchNodeTest
    {
        /// <summary>验证空业务节点具有稳定名称并立即成功。</summary>
        [TestCase("CheckDownloadServerConfigSuccessNode", "CheckDownloadServerConfigSuccess")]
        [TestCase("BeforeLoginGlobalResLoadNode", "BeforeLoginGlobalResLoad")]
        [TestCase("DownloadServerDynamicNode", "DownloadServerDynamic")]
        [TestCase("CheckDownloadServerDynamicSuccessNode", "CheckDownloadServerDynamicSuccess")]
        [TestCase("SDKInitNode", "SDKInit")]
        [TestCase("SDKInitCheckNode", "SDKInitCheck")]
        [TestCase("CheckVersionNode", "CheckVersion")]
        [TestCase("UpdateNode", "Update")]
        [TestCase("ReloadResNode", "ReloadRes")]
        [TestCase("CompileShaderNode", "CompileShader")]
        [TestCase("CheckSubpackUpdateNode", "CheckSubpackUpdate")]
        [TestCase("LaunchGCNode", "LaunchGC")]
        [TestCase("OpenLoginPanelNode", "OpenLoginPanel")]
        [TestCase("SDKLoginNode", "SDKLogin")]
        [TestCase("EndNode", "End")]
        public void ImmediateNodeSucceeds(string nodeTypeName, string expectedName)
        {
            Type nodeType = typeof(LaunchNode).Assembly.GetType("ST.Core.Launch." + nodeTypeName);
            Assert.IsNotNull(nodeType, nodeTypeName);
            LaunchNode node = (LaunchNode)Activator.CreateInstance(nodeType);
            node.Start();

            Assert.AreEqual(expectedName, node.name);
            Assert.IsTrue(node.isSucceeded);
        }
    }
}
```

### LaunchModuleTest.cs

```csharp
using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;

namespace ST.Core.Launch.Tests
{
    /// <summary>
    /// 启动模块节点映射与树结构测试。
    /// </summary>
    public sealed class LaunchModuleTest
    {
        /// <summary>验证有效枚举映射到对应节点类型。</summary>
        [TestCase(LaunchNodeType.WaitFrame, typeof(WaitFrameNode))]
        [TestCase(LaunchNodeType.AppInit, typeof(AppInitNode))]
        [TestCase(LaunchNodeType.OpenLaunchShell, typeof(OpenLaunchShellNode))]
        [TestCase(LaunchNodeType.SplashGlobalResLoad, typeof(SplashGlobalResLoadNode))]
        [TestCase(LaunchNodeType.DownloadServerConfig, typeof(DownloadServerConfigNode))]
        [TestCase(LaunchNodeType.TableLoad, typeof(TableLoadNode))]
        [TestCase(LaunchNodeType.CheckDownloadServerConfigSuccess, typeof(CheckDownloadServerConfigSuccessNode))]
        [TestCase(LaunchNodeType.BeforeLoginGlobalResLoad, typeof(BeforeLoginGlobalResLoadNode))]
        [TestCase(LaunchNodeType.DownloadServerDynamic, typeof(DownloadServerDynamicNode))]
        [TestCase(LaunchNodeType.CheckDownloadServerDynamicSuccess, typeof(CheckDownloadServerDynamicSuccessNode))]
        [TestCase(LaunchNodeType.SDKInit, typeof(SDKInitNode))]
        [TestCase(LaunchNodeType.SDKInitCheck, typeof(SDKInitCheckNode))]
        [TestCase(LaunchNodeType.CheckVersion, typeof(CheckVersionNode))]
        [TestCase(LaunchNodeType.Update, typeof(UpdateNode))]
        [TestCase(LaunchNodeType.ReloadRes, typeof(ReloadResNode))]
        [TestCase(LaunchNodeType.CompileShader, typeof(CompileShaderNode))]
        [TestCase(LaunchNodeType.CheckSubpackUpdate, typeof(CheckSubpackUpdateNode))]
        [TestCase(LaunchNodeType.LaunchGC, typeof(LaunchGCNode))]
        [TestCase(LaunchNodeType.OpenLoginPanel, typeof(OpenLoginPanelNode))]
        [TestCase(LaunchNodeType.SDKLogin, typeof(SDKLoginNode))]
        [TestCase(LaunchNodeType.End, typeof(EndNode))]
        public void FactoryMapsNodeType(LaunchNodeType nodeType, Type expectedType)
        {
            LaunchNode node = InvokeCreateNode(new LaunchModule(), nodeType);
            Assert.AreEqual(expectedType, node.GetType());
        }

        /// <summary>验证无效枚举被拒绝。</summary>
        [TestCase(LaunchNodeType.None)]
        [TestCase(LaunchNodeType.Max)]
        [TestCase((LaunchNodeType)byte.MaxValue)]
        public void FactoryRejectsInvalidType(LaunchNodeType nodeType)
        {
            TargetInvocationException exception = Assert.Throws<TargetInvocationException>(
                () => InvokeCreateNode(new LaunchModule(), nodeType));
            Assert.IsInstanceOf<ArgumentOutOfRangeException>(exception.InnerException);
        }

        /// <summary>验证标准根节点和首屏并行组顺序。</summary>
        [Test]
        public void LaunchBuildsExactTree()
        {
            LaunchModule module = new LaunchModule();
            module.Launch();

            SequenceNode root = (SequenceNode)GetField(module, "m_RootNode");
            List<LaunchNode> rootChildren = (List<LaunchNode>)GetField(root, "m_Children");
            Assert.AreEqual("LaunchNode", root.name);
            Assert.AreEqual(18, rootChildren.Count);
            Assert.AreEqual(typeof(WaitFrameNode), rootChildren[0].GetType());
            Assert.AreEqual(typeof(AppInitNode), rootChildren[1].GetType());
            Assert.AreEqual(typeof(ParallelNode), rootChildren[2].GetType());
            Assert.AreEqual(typeof(CheckDownloadServerConfigSuccessNode), rootChildren[3].GetType());
            Assert.AreEqual(typeof(BeforeLoginGlobalResLoadNode), rootChildren[4].GetType());
            Assert.AreEqual(typeof(DownloadServerDynamicNode), rootChildren[5].GetType());
            Assert.AreEqual(typeof(CheckDownloadServerDynamicSuccessNode), rootChildren[6].GetType());
            Assert.AreEqual(typeof(SDKInitNode), rootChildren[7].GetType());
            Assert.AreEqual(typeof(SDKInitCheckNode), rootChildren[8].GetType());
            Assert.AreEqual(typeof(CheckVersionNode), rootChildren[9].GetType());
            Assert.AreEqual(typeof(UpdateNode), rootChildren[10].GetType());
            Assert.AreEqual(typeof(ReloadResNode), rootChildren[11].GetType());
            Assert.AreEqual(typeof(CompileShaderNode), rootChildren[12].GetType());
            Assert.AreEqual(typeof(CheckSubpackUpdateNode), rootChildren[13].GetType());
            Assert.AreEqual(typeof(LaunchGCNode), rootChildren[14].GetType());
            Assert.AreEqual(typeof(OpenLoginPanelNode), rootChildren[15].GetType());
            Assert.AreEqual(typeof(SDKLoginNode), rootChildren[16].GetType());
            Assert.AreEqual(typeof(EndNode), rootChildren[17].GetType());

            ParallelNode splash = (ParallelNode)rootChildren[2];
            List<LaunchNode> splashChildren = (List<LaunchNode>)GetField(splash, "m_Children");
            Assert.AreEqual("Splash", splash.name);
            Assert.AreEqual(4, splashChildren.Count);
            Assert.AreEqual(typeof(OpenLaunchShellNode), splashChildren[0].GetType());
            Assert.AreEqual(typeof(SplashGlobalResLoadNode), splashChildren[1].GetType());
            Assert.AreEqual(typeof(DownloadServerConfigNode), splashChildren[2].GetType());
            Assert.AreEqual(typeof(TableLoadNode), splashChildren[3].GetType());
        }

        /// <summary>验证模块从启动到一帧后成功。</summary>
        [Test]
        public void LaunchCompletesAfterOneUpdate()
        {
            LaunchModule module = new LaunchModule();
            module.DoUpdate();
            Assert.AreEqual(LaunchNodeState.Ready, module.state);
            module.Launch();
            Assert.AreEqual(LaunchNodeState.Running, module.state);
            object root = GetField(module, "m_RootNode");
            module.Launch();
            Assert.AreSame(root, GetField(module, "m_RootNode"));
            module.DoUpdate();
            Assert.IsTrue(module.isSucceeded);
            module.DoUpdate();
            Assert.IsTrue(module.isSucceeded);
        }

        /// <summary>验证关闭状态转换和幂等。</summary>
        [Test]
        public void ClosePreservesTerminalState()
        {
            LaunchModule ready = new LaunchModule();
            ready.DoClose();
            ready.DoClose();
            Assert.AreEqual(LaunchNodeState.Stopped, ready.state);

            LaunchModule running = new LaunchModule();
            running.Launch();
            running.DoClose();
            Assert.AreEqual(LaunchNodeState.Stopped, running.state);

            LaunchModule succeeded = new LaunchModule();
            succeeded.Launch();
            succeeded.DoUpdate();
            succeeded.DoClose();
            Assert.AreEqual(LaunchNodeState.Succeeded, succeeded.state);
        }

        /// <summary>通过反射调用私有节点工厂。</summary>
        static LaunchNode InvokeCreateNode(LaunchModule module, LaunchNodeType nodeType)
        {
            MethodInfo method = typeof(LaunchModule).GetMethod(
                "CreateNode",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method);
            return (LaunchNode)method.Invoke(module, new object[] { nodeType });
        }

        /// <summary>读取对象的非公开实例字段。</summary>
        static object GetField(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field);
            return field.GetValue(target);
        }
    }
}
```

### GameApplicationTest.cs

```csharp
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace ST.Core.Launch.Tests
{
    /// <summary>
    /// 游戏运行入口与场景启动组件测试。
    /// </summary>
    public sealed class GameApplicationTest
    {
        /// <summary>验证初始化前帧调用安全。</summary>
        [Test]
        public void BeforeInitIsSafe()
        {
            GameApplication application = new GameApplication();
            application.DoUpdate();
            application.DoLateUpdate();
            Assert.IsNull(application.launchModule);
        }

        /// <summary>验证初始化创建并启动唯一模块。</summary>
        [Test]
        public void InitCreatesOneLaunchModule()
        {
            GameApplication application = new GameApplication();
            application.DoInit();
            LaunchModule module = application.launchModule;
            application.DoInit();

            Assert.IsNotNull(module);
            Assert.AreSame(module, application.launchModule);
            Assert.AreEqual(LaunchNodeState.Running, module.state);
        }

        /// <summary>验证应用逐帧驱动启动完成。</summary>
        [Test]
        public void UpdateCompletesLaunch()
        {
            GameApplication application = new GameApplication();
            application.DoInit();
            application.DoUpdate();
            Assert.IsTrue(application.launchModule.isSucceeded);
        }

        /// <summary>验证运行中关闭和重复关闭安全。</summary>
        [Test]
        public void CloseRunningApplicationIsIdempotent()
        {
            GameApplication application = new GameApplication();
            application.DoInit();
            application.DoClose();
            application.DoClose();
            application.DoUpdate();
            application.DoLateUpdate();
            Assert.AreEqual(LaunchNodeState.Stopped, application.launchModule.state);
        }

        /// <summary>验证成功后关闭保留成功状态。</summary>
        [Test]
        public void CloseAfterSuccessPreservesSuccess()
        {
            GameApplication application = new GameApplication();
            application.DoInit();
            application.DoUpdate();
            application.DoClose();
            Assert.AreEqual(LaunchNodeState.Succeeded, application.launchModule.state);
        }

        /// <summary>验证场景入口的公开类型和生命周期方法。</summary>
        [Test]
        public void GameLauncherHasUnityLifecycle()
        {
            TypeInfo typeInfo = typeof(GameLauncher).GetTypeInfo();
            Assert.IsTrue(typeInfo.IsPublic);
            Assert.IsTrue(typeInfo.IsSealed);
            Assert.IsTrue(typeof(MonoBehaviour).IsAssignableFrom(typeof(GameLauncher)));
            Assert.IsNotNull(GetLifecycleMethod("Awake"));
            Assert.IsNotNull(GetLifecycleMethod("Update"));
            Assert.IsNotNull(GetLifecycleMethod("LateUpdate"));
            Assert.IsNotNull(GetLifecycleMethod("OnDestroy"));
        }

        /// <summary>读取指定 Unity 生命周期方法。</summary>
        static MethodInfo GetLifecycleMethod(string methodName)
        {
            return typeof(GameLauncher).GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
        }
    }
}
```

### CheckVersionNode.cs

```csharp
namespace ST.Core.Launch
{
    /// <summary>版本检查占位节点。</summary>
    public sealed class CheckVersionNode : LaunchNode
    {
        /// <summary>创建版本检查节点。</summary>
        public CheckVersionNode() : base("CheckVersion") { }

        /// <summary>完成占位操作。</summary>
        protected override void OnStart()
        {
            Complete();
        }
    }
}
```

### UpdateNode.cs

```csharp
namespace ST.Core.Launch
{
    /// <summary>资源更新占位节点。</summary>
    public sealed class UpdateNode : LaunchNode
    {
        /// <summary>创建资源更新节点。</summary>
        public UpdateNode() : base("Update") { }

        /// <summary>完成占位操作。</summary>
        protected override void OnStart()
        {
            Complete();
        }
    }
}
```

### ReloadResNode.cs

```csharp
namespace ST.Core.Launch
{
    /// <summary>资源重载占位节点。</summary>
    public sealed class ReloadResNode : LaunchNode
    {
        /// <summary>创建资源重载节点。</summary>
        public ReloadResNode() : base("ReloadRes") { }

        /// <summary>完成占位操作。</summary>
        protected override void OnStart()
        {
            Complete();
        }
    }
}
```

### CompileShaderNode.cs

```csharp
namespace ST.Core.Launch
{
    /// <summary>着色器预编译占位节点。</summary>
    public sealed class CompileShaderNode : LaunchNode
    {
        /// <summary>创建着色器预编译节点。</summary>
        public CompileShaderNode() : base("CompileShader") { }

        /// <summary>完成占位操作。</summary>
        protected override void OnStart()
        {
            Complete();
        }
    }
}
```

### CheckSubpackUpdateNode.cs

```csharp
namespace ST.Core.Launch
{
    /// <summary>分包更新检查占位节点。</summary>
    public sealed class CheckSubpackUpdateNode : LaunchNode
    {
        /// <summary>创建分包更新检查节点。</summary>
        public CheckSubpackUpdateNode() : base("CheckSubpackUpdate") { }

        /// <summary>完成占位操作。</summary>
        protected override void OnStart()
        {
            Complete();
        }
    }
}
```

### LaunchGCNode.cs

```csharp
namespace ST.Core.Launch
{
    /// <summary>启动阶段垃圾回收占位节点。</summary>
    public sealed class LaunchGCNode : LaunchNode
    {
        /// <summary>创建启动阶段垃圾回收节点。</summary>
        public LaunchGCNode() : base("LaunchGC") { }

        /// <summary>完成占位操作。</summary>
        protected override void OnStart()
        {
            Complete();
        }
    }
}
```

### OpenLoginPanelNode.cs

```csharp
namespace ST.Core.Launch
{
    /// <summary>登录界面打开占位节点。</summary>
    public sealed class OpenLoginPanelNode : LaunchNode
    {
        /// <summary>创建登录界面打开节点。</summary>
        public OpenLoginPanelNode() : base("OpenLoginPanel") { }

        /// <summary>完成占位操作。</summary>
        protected override void OnStart()
        {
            Complete();
        }
    }
}
```

### SDKLoginNode.cs

```csharp
namespace ST.Core.Launch
{
    /// <summary>SDK 登录占位节点。</summary>
    public sealed class SDKLoginNode : LaunchNode
    {
        /// <summary>创建 SDK 登录节点。</summary>
        public SDKLoginNode() : base("SDKLogin") { }

        /// <summary>完成占位操作。</summary>
        protected override void OnStart()
        {
            Complete();
        }
    }
}
```

### EndNode.cs

```csharp
namespace ST.Core.Launch
{
    /// <summary>启动流程结束占位节点。</summary>
    public sealed class EndNode : LaunchNode
    {
        /// <summary>创建启动流程结束节点。</summary>
        public EndNode() : base("End") { }

        /// <summary>完成占位操作。</summary>
        protected override void OnStart()
        {
            Complete();
        }
    }
}
```

### CheckDownloadServerConfigSuccessNode.cs through SDKInitCheckNode.cs

The complete listings for these files are the identically named code blocks already present in this
chunk. No node file shares a class.

---

## Chunk 6: Exact enum and concrete-node listings

### LaunchNodeType.cs

```csharp
namespace ST.Core.Launch
{
    /// <summary>
    /// 标准启动节点类型。
    /// </summary>
    public enum LaunchNodeType : byte
    {
        /// <summary>无效类型。</summary>
        None,
        /// <summary>等待一帧。</summary>
        WaitFrame,
        /// <summary>初始化应用。</summary>
        AppInit,
        /// <summary>打开启动界面。</summary>
        OpenLaunchShell,
        /// <summary>加载首屏全局资源。</summary>
        SplashGlobalResLoad,
        /// <summary>下载服务器配置。</summary>
        DownloadServerConfig,
        /// <summary>加载数据表。</summary>
        TableLoad,
        /// <summary>检查服务器配置下载结果。</summary>
        CheckDownloadServerConfigSuccess,
        /// <summary>加载登录前全局资源。</summary>
        BeforeLoginGlobalResLoad,
        /// <summary>下载服务器动态配置。</summary>
        DownloadServerDynamic,
        /// <summary>检查动态配置下载结果。</summary>
        CheckDownloadServerDynamicSuccess,
        /// <summary>初始化 SDK。</summary>
        SDKInit,
        /// <summary>检查 SDK 初始化结果。</summary>
        SDKInitCheck,
        /// <summary>检查版本。</summary>
        CheckVersion,
        /// <summary>执行资源更新。</summary>
        Update,
        /// <summary>重新加载资源。</summary>
        ReloadRes,
        /// <summary>预编译着色器。</summary>
        CompileShader,
        /// <summary>检查分包更新。</summary>
        CheckSubpackUpdate,
        /// <summary>执行启动阶段垃圾回收。</summary>
        LaunchGC,
        /// <summary>打开登录界面。</summary>
        OpenLoginPanel,
        /// <summary>执行 SDK 登录。</summary>
        SDKLogin,
        /// <summary>结束启动流程。</summary>
        End,
        /// <summary>类型数量边界。</summary>
        Max,
    }
}
```

Every listing below is the complete content of the identically named file.

### WaitFrameNode.cs

```csharp
namespace ST.Core.Launch
{
    /// <summary>等待一个 Unity 更新帧。</summary>
    public sealed class WaitFrameNode : LaunchNode
    {
        /// <summary>创建等待帧节点。</summary>
        public WaitFrameNode() : base("WaitFrame") { }

        /// <summary>启动时保持运行，等待下一帧。</summary>
        protected override void OnStart() { }

        /// <summary>首个更新帧完成节点。</summary>
        protected override void OnUpdate()
        {
            Complete();
        }
    }
}
```

### AppInitNode.cs

```csharp
namespace ST.Core.Launch
{
    /// <summary>应用初始化占位节点。</summary>
    public sealed class AppInitNode : LaunchNode
    {
        /// <summary>创建应用初始化节点。</summary>
        public AppInitNode() : base("AppInit") { }

        /// <summary>完成占位初始化。</summary>
        protected override void OnStart()
        {
            Complete();
        }
    }
}
```

### OpenLaunchShellNode.cs

```csharp
namespace ST.Core.Launch
{
    /// <summary>打开启动界面占位节点。</summary>
    public sealed class OpenLaunchShellNode : LaunchNode
    {
        /// <summary>创建打开启动界面节点。</summary>
        public OpenLaunchShellNode() : base("OpenLaunchShell") { }

        /// <summary>完成占位操作。</summary>
        protected override void OnStart()
        {
            Complete();
        }
    }
}
```

### SplashGlobalResLoadNode.cs

```csharp
namespace ST.Core.Launch
{
    /// <summary>首屏全局资源加载占位节点。</summary>
    public sealed class SplashGlobalResLoadNode : LaunchNode
    {
        /// <summary>创建首屏全局资源加载节点。</summary>
        public SplashGlobalResLoadNode() : base("SplashGlobalResLoad") { }

        /// <summary>完成占位操作。</summary>
        protected override void OnStart()
        {
            Complete();
        }
    }
}
```

### DownloadServerConfigNode.cs

```csharp
namespace ST.Core.Launch
{
    /// <summary>服务器配置下载占位节点。</summary>
    public sealed class DownloadServerConfigNode : LaunchNode
    {
        /// <summary>创建服务器配置下载节点。</summary>
        public DownloadServerConfigNode() : base("DownloadServerConfig") { }

        /// <summary>完成占位操作。</summary>
        protected override void OnStart()
        {
            Complete();
        }
    }
}
```

### TableLoadNode.cs

```csharp
namespace ST.Core.Launch
{
    /// <summary>数据表加载占位节点。</summary>
    public sealed class TableLoadNode : LaunchNode
    {
        /// <summary>创建数据表加载节点。</summary>
        public TableLoadNode() : base("TableLoad") { }

        /// <summary>完成占位操作。</summary>
        protected override void OnStart()
        {
            Complete();
        }
    }
}
```

### CheckDownloadServerConfigSuccessNode.cs

```csharp
namespace ST.Core.Launch
{
    /// <summary>服务器配置下载结果检查占位节点。</summary>
    public sealed class CheckDownloadServerConfigSuccessNode : LaunchNode
    {
        /// <summary>创建服务器配置下载结果检查节点。</summary>
        public CheckDownloadServerConfigSuccessNode() : base("CheckDownloadServerConfigSuccess") { }

        /// <summary>完成占位操作。</summary>
        protected override void OnStart()
        {
            Complete();
        }
    }
}
```

### BeforeLoginGlobalResLoadNode.cs

```csharp
namespace ST.Core.Launch
{
    /// <summary>登录前全局资源加载占位节点。</summary>
    public sealed class BeforeLoginGlobalResLoadNode : LaunchNode
    {
        /// <summary>创建登录前全局资源加载节点。</summary>
        public BeforeLoginGlobalResLoadNode() : base("BeforeLoginGlobalResLoad") { }

        /// <summary>完成占位操作。</summary>
        protected override void OnStart()
        {
            Complete();
        }
    }
}
```

### DownloadServerDynamicNode.cs

```csharp
namespace ST.Core.Launch
{
    /// <summary>服务器动态配置下载占位节点。</summary>
    public sealed class DownloadServerDynamicNode : LaunchNode
    {
        /// <summary>创建服务器动态配置下载节点。</summary>
        public DownloadServerDynamicNode() : base("DownloadServerDynamic") { }

        /// <summary>完成占位操作。</summary>
        protected override void OnStart()
        {
            Complete();
        }
    }
}
```

### CheckDownloadServerDynamicSuccessNode.cs

```csharp
namespace ST.Core.Launch
{
    /// <summary>服务器动态配置下载结果检查占位节点。</summary>
    public sealed class CheckDownloadServerDynamicSuccessNode : LaunchNode
    {
        /// <summary>创建服务器动态配置下载结果检查节点。</summary>
        public CheckDownloadServerDynamicSuccessNode() : base("CheckDownloadServerDynamicSuccess") { }

        /// <summary>完成占位操作。</summary>
        protected override void OnStart()
        {
            Complete();
        }
    }
}
```

### SDKInitNode.cs

```csharp
namespace ST.Core.Launch
{
    /// <summary>SDK 初始化占位节点。</summary>
    public sealed class SDKInitNode : LaunchNode
    {
        /// <summary>创建 SDK 初始化节点。</summary>
        public SDKInitNode() : base("SDKInit") { }

        /// <summary>完成占位操作。</summary>
        protected override void OnStart()
        {
            Complete();
        }
    }
}
```

### SDKInitCheckNode.cs

```csharp
namespace ST.Core.Launch
{
    /// <summary>SDK 初始化结果检查占位节点。</summary>
    public sealed class SDKInitCheckNode : LaunchNode
    {
        /// <summary>创建 SDK 初始化结果检查节点。</summary>
        public SDKInitCheckNode() : base("SDKInitCheck") { }

        /// <summary>完成占位操作。</summary>
        protected override void OnStart()
        {
            Complete();
        }
    }
}
```

---

## Chunk 5: Exact core test listings

The following listings are the exact NUnit contents required by Tasks 1–3.

### ProbeLaunchNode.cs

```csharp
using System;
using System.Collections.Generic;

namespace ST.Core.Launch.Tests
{
    /// <summary>
    /// 可配置的启动节点测试替身。
    /// </summary>
    public sealed class ProbeLaunchNode : LaunchNode
    {
        /// <summary>可选的执行顺序记录容器。</summary>
        readonly List<string> m_ExecutionOrder;

        /// <summary>启动调用次数。</summary>
        public int startCount { get; private set; }

        /// <summary>更新调用次数。</summary>
        public int updateCount { get; private set; }

        /// <summary>停止调用次数。</summary>
        public int stopCount { get; private set; }

        /// <summary>是否在启动时成功。</summary>
        public bool completeOnStart { get; set; }

        /// <summary>是否在启动时失败。</summary>
        public bool failOnStart { get; set; }

        /// <summary>是否在更新时成功。</summary>
        public bool completeOnUpdate { get; set; }

        /// <summary>是否在更新时失败。</summary>
        public bool failOnUpdate { get; set; }

        /// <summary>是否在启动时抛出异常。</summary>
        public bool throwOnStart { get; set; }

        /// <summary>是否在更新时抛出异常。</summary>
        public bool throwOnUpdate { get; set; }

        /// <summary>是否在停止时抛出异常。</summary>
        public bool throwOnStop { get; set; }

        /// <summary>失败时传入的错误信息。</summary>
        public string failureMessage { get; set; } = "Probe failure.";

        /// <summary>
        /// 创建测试节点。
        /// </summary>
        /// <param name="name">节点名称。</param>
        /// <param name="executionOrder">可选执行顺序容器。</param>
        public ProbeLaunchNode(string name, List<string> executionOrder = null) : base(name)
        {
            m_ExecutionOrder = executionOrder;
        }

        /// <summary>执行配置的启动行为。</summary>
        protected override void OnStart()
        {
            ++this.startCount;
            if (m_ExecutionOrder != null)
            {
                m_ExecutionOrder.Add(this.name);
            }

            if (this.throwOnStart)
            {
                throw new InvalidOperationException("Start exception.");
            }

            if (this.failOnStart)
            {
                Fail(this.failureMessage);
                return;
            }

            if (this.completeOnStart)
            {
                Complete();
            }
        }

        /// <summary>执行配置的更新行为。</summary>
        protected override void OnUpdate()
        {
            ++this.updateCount;
            if (this.throwOnUpdate)
            {
                throw new InvalidOperationException("Update exception.");
            }

            if (this.failOnUpdate)
            {
                Fail(this.failureMessage);
                return;
            }

            if (this.completeOnUpdate)
            {
                Complete();
            }
        }

        /// <summary>执行配置的停止行为。</summary>
        protected override void OnStop()
        {
            ++this.stopCount;
            if (this.throwOnStop)
            {
                throw new InvalidOperationException("Stop exception.");
            }
        }
    }
}
```

### LaunchNodeTest.cs

```csharp
using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ST.Core.Launch.Tests
{
    /// <summary>
    /// 启动节点基础状态测试。
    /// </summary>
    public sealed class LaunchNodeTest
    {
        /// <summary>验证空名称被拒绝。</summary>
        [Test]
        public void ConstructorRejectsEmptyName()
        {
            Assert.Throws<ArgumentException>(() => new ProbeLaunchNode(null));
            Assert.Throws<ArgumentException>(() => new ProbeLaunchNode(string.Empty));
        }

        /// <summary>验证初始状态与更新保护。</summary>
        [Test]
        public void NewNodeIsReadyAndIgnoresUpdate()
        {
            ProbeLaunchNode node = new ProbeLaunchNode("Probe");
            node.DoUpdate();

            Assert.AreEqual(LaunchNodeState.Ready, node.state);
            Assert.AreEqual(string.Empty, node.error);
            Assert.IsFalse(node.isDone);
            Assert.IsFalse(node.isSucceeded);
            Assert.AreEqual(0, node.updateCount);
        }

        /// <summary>验证启动幂等。</summary>
        [Test]
        public void StartExecutesOnce()
        {
            ProbeLaunchNode node = new ProbeLaunchNode("Probe");
            node.Start();
            node.Start();

            Assert.AreEqual(LaunchNodeState.Running, node.state);
            Assert.AreEqual(1, node.startCount);
        }

        /// <summary>验证启动异常转换为失败。</summary>
        [Test]
        public void StartExceptionFailsNode()
        {
            ProbeLaunchNode node = new ProbeLaunchNode("Probe") { throwOnStart = true };
            node.Start();

            Assert.AreEqual(LaunchNodeState.Failed, node.state);
            StringAssert.Contains("Start exception.", node.error);
        }

        /// <summary>验证更新异常转换为失败。</summary>
        [Test]
        public void UpdateExceptionFailsNode()
        {
            ProbeLaunchNode node = new ProbeLaunchNode("Probe") { throwOnUpdate = true };
            node.Start();
            node.DoUpdate();

            Assert.AreEqual(LaunchNodeState.Failed, node.state);
            StringAssert.Contains("Update exception.", node.error);
        }

        /// <summary>验证空失败文本被标准化。</summary>
        [TestCase(null)]
        [TestCase("")]
        public void EmptyFailureUsesDefaultMessage(string error)
        {
            ProbeLaunchNode node = new ProbeLaunchNode("Probe")
            {
                failOnStart = true,
                failureMessage = error,
            };
            node.Start();

            Assert.AreEqual("Launch node failed.", node.error);
        }

        /// <summary>验证就绪状态停止不触发回调。</summary>
        [Test]
        public void StopReadyNodeSkipsCallback()
        {
            ProbeLaunchNode node = new ProbeLaunchNode("Probe");
            node.Stop();

            Assert.AreEqual(LaunchNodeState.Stopped, node.state);
            Assert.AreEqual(0, node.stopCount);
        }

        /// <summary>验证运行状态停止触发一次回调。</summary>
        [Test]
        public void StopRunningNodeExecutesOnce()
        {
            ProbeLaunchNode node = new ProbeLaunchNode("Probe");
            node.Start();
            node.Stop();
            node.Stop();

            Assert.AreEqual(LaunchNodeState.Stopped, node.state);
            Assert.AreEqual(1, node.stopCount);
        }

        /// <summary>验证停止异常不会逃逸。</summary>
        [Test]
        public void StopExceptionStillStopsNode()
        {
            ProbeLaunchNode node = new ProbeLaunchNode("Probe") { throwOnStop = true };
            node.Start();

            LogAssert.Expect(LogType.Exception, "InvalidOperationException: Stop exception.");
            Assert.DoesNotThrow(node.Stop);
            Assert.AreEqual(LaunchNodeState.Stopped, node.state);
        }

        /// <summary>验证成功终态忽略后续生命周期调用。</summary>
        [Test]
        public void TerminalNodeIgnoresFurtherCalls()
        {
            ProbeLaunchNode node = new ProbeLaunchNode("Probe") { completeOnStart = true };
            node.Start();
            node.Start();
            node.DoUpdate();
            node.Stop();

            Assert.AreEqual(LaunchNodeState.Succeeded, node.state);
            Assert.AreEqual(1, node.startCount);
            Assert.AreEqual(0, node.updateCount);
            Assert.AreEqual(0, node.stopCount);
        }
    }
}
```

### SequenceNodeTest.cs

```csharp
using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace ST.Core.Launch.Tests
{
    /// <summary>
    /// 顺序启动节点调度测试。
    /// </summary>
    public sealed class SequenceNodeTest
    {
        /// <summary>验证空顺序节点立即成功。</summary>
        [Test]
        public void EmptySequenceSucceeds()
        {
            SequenceNode sequence = new SequenceNode("Sequence");
            sequence.Start();
            Assert.IsTrue(sequence.isSucceeded);
        }

        /// <summary>验证同步节点按顺序执行。</summary>
        [Test]
        public void SynchronousChildrenRunInOrder()
        {
            List<string> order = new List<string>();
            SequenceNode sequence = new SequenceNode("Sequence");
            sequence.Append(new ProbeLaunchNode("First", order) { completeOnStart = true });
            sequence.Append(new ProbeLaunchNode("Second", order) { completeOnStart = true });

            sequence.Start();

            CollectionAssert.AreEqual(new[] { "First", "Second" }, order);
            Assert.IsTrue(sequence.isSucceeded);
        }

        /// <summary>验证运行节点阻塞后续节点。</summary>
        [Test]
        public void RunningChildBlocksUntilUpdate()
        {
            ProbeLaunchNode first = new ProbeLaunchNode("First") { completeOnUpdate = true };
            ProbeLaunchNode second = new ProbeLaunchNode("Second") { completeOnStart = true };
            SequenceNode sequence = new SequenceNode("Sequence");
            sequence.Append(first);
            sequence.Append(second);

            sequence.Start();
            Assert.AreEqual(0, second.startCount);
            sequence.DoUpdate();

            Assert.AreEqual(1, second.startCount);
            Assert.IsTrue(sequence.isSucceeded);
        }

        /// <summary>验证异步失败阻断后续节点并保留错误。</summary>
        [Test]
        public void AsynchronousFailureStopsSequence()
        {
            ProbeLaunchNode first = new ProbeLaunchNode("First")
            {
                failOnUpdate = true,
                failureMessage = "First failed.",
            };
            ProbeLaunchNode second = new ProbeLaunchNode("Second") { completeOnStart = true };
            SequenceNode sequence = new SequenceNode("Sequence");
            sequence.Append(first);
            sequence.Append(second);

            sequence.Start();
            sequence.DoUpdate();

            Assert.AreEqual(LaunchNodeState.Failed, sequence.state);
            Assert.AreEqual("First failed.", sequence.error);
            Assert.AreEqual(0, second.startCount);
        }

        /// <summary>验证停止传播到当前运行节点。</summary>
        [Test]
        public void StopPropagatesToRunningChild()
        {
            ProbeLaunchNode child = new ProbeLaunchNode("Child");
            SequenceNode sequence = new SequenceNode("Sequence");
            sequence.Append(child);
            sequence.Start();
            sequence.Stop();

            Assert.AreEqual(LaunchNodeState.Stopped, child.state);
            Assert.AreEqual(1, child.stopCount);
        }

        /// <summary>验证追加参数和状态检查。</summary>
        [Test]
        public void AppendValidatesInputAndState()
        {
            SequenceNode sequence = new SequenceNode("Sequence");
            Assert.Throws<ArgumentNullException>(() => sequence.Append(null));
            sequence.Start();
            Assert.Throws<InvalidOperationException>(
                () => sequence.Append(new ProbeLaunchNode("Late")));
        }
    }
}
```

### ParallelNodeTest.cs

```csharp
using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace ST.Core.Launch.Tests
{
    /// <summary>
    /// 并行启动节点调度测试。
    /// </summary>
    public sealed class ParallelNodeTest
    {
        /// <summary>验证空并行节点立即成功。</summary>
        [Test]
        public void EmptyParallelSucceeds()
        {
            ParallelNode parallel = new ParallelNode("Parallel");
            parallel.Start();
            Assert.IsTrue(parallel.isSucceeded);
        }

        /// <summary>验证全部子节点按追加顺序启动。</summary>
        [Test]
        public void ChildrenStartInOrder()
        {
            List<string> order = new List<string>();
            ParallelNode parallel = new ParallelNode("Parallel");
            parallel.Append(new ProbeLaunchNode("First", order) { completeOnStart = true });
            parallel.Append(new ProbeLaunchNode("Second", order) { completeOnStart = true });
            parallel.Start();

            CollectionAssert.AreEqual(new[] { "First", "Second" }, order);
            Assert.IsTrue(parallel.isSucceeded);
        }

        /// <summary>验证并行节点等待全部子节点。</summary>
        [Test]
        public void ParallelWaitsForEveryChild()
        {
            ParallelNode parallel = new ParallelNode("Parallel");
            parallel.Append(new ProbeLaunchNode("First") { completeOnStart = true });
            parallel.Append(new ProbeLaunchNode("Second") { completeOnUpdate = true });
            parallel.Start();
            Assert.AreEqual(LaunchNodeState.Running, parallel.state);
            parallel.DoUpdate();
            Assert.IsTrue(parallel.isSucceeded);
        }

        /// <summary>验证异步失败停止运行中的兄弟节点。</summary>
        [Test]
        public void AsynchronousFailureStopsSibling()
        {
            ProbeLaunchNode running = new ProbeLaunchNode("Running");
            ProbeLaunchNode failing = new ProbeLaunchNode("Failing")
            {
                failOnUpdate = true,
                failureMessage = "Parallel failed.",
            };
            ParallelNode parallel = new ParallelNode("Parallel");
            parallel.Append(running);
            parallel.Append(failing);
            parallel.Start();
            parallel.DoUpdate();

            Assert.AreEqual(LaunchNodeState.Failed, parallel.state);
            Assert.AreEqual("Parallel failed.", parallel.error);
            Assert.AreEqual(LaunchNodeState.Stopped, running.state);
        }

        /// <summary>验证同步失败阻止后续节点启动。</summary>
        [Test]
        public void SynchronousFailurePreventsLaterStart()
        {
            ProbeLaunchNode running = new ProbeLaunchNode("Running");
            ProbeLaunchNode failing = new ProbeLaunchNode("Failing") { failOnStart = true };
            ProbeLaunchNode later = new ProbeLaunchNode("Later") { completeOnStart = true };
            ParallelNode parallel = new ParallelNode("Parallel");
            parallel.Append(running);
            parallel.Append(failing);
            parallel.Append(later);
            parallel.Start();

            Assert.AreEqual(LaunchNodeState.Failed, parallel.state);
            Assert.AreEqual(LaunchNodeState.Stopped, running.state);
            Assert.AreEqual(0, later.startCount);
        }

        /// <summary>验证父节点停止传播到全部运行节点。</summary>
        [Test]
        public void StopPropagatesToRunningChildren()
        {
            ProbeLaunchNode first = new ProbeLaunchNode("First");
            ProbeLaunchNode second = new ProbeLaunchNode("Second");
            ParallelNode parallel = new ParallelNode("Parallel");
            parallel.Append(first);
            parallel.Append(second);
            parallel.Start();
            parallel.Stop();

            Assert.AreEqual(LaunchNodeState.Stopped, first.state);
            Assert.AreEqual(LaunchNodeState.Stopped, second.state);
        }

        /// <summary>验证追加参数和状态检查。</summary>
        [Test]
        public void AppendValidatesInputAndState()
        {
            ParallelNode parallel = new ParallelNode("Parallel");
            Assert.Throws<ArgumentNullException>(() => parallel.Append(null));
            parallel.Start();
            Assert.Throws<InvalidOperationException>(
                () => parallel.Append(new ProbeLaunchNode("Late")));
        }
    }
}
```

### LaunchNode.cs

```csharp
using System;
using ST.Core.Logging;

namespace ST.Core.Launch
{
    /// <summary>
    /// 启动节点基类，统一管理执行状态与异常边界。
    /// </summary>
    public abstract class LaunchNode
    {
        /// <summary>节点稳定名称。</summary>
        readonly string m_Name;

        /// <summary>当前生命周期状态。</summary>
        LaunchNodeState m_State;

        /// <summary>失败信息；无错误时为空字符串。</summary>
        string m_Error = string.Empty;

        /// <summary>节点稳定名称。</summary>
        public string name { get { return m_Name; } }

        /// <summary>当前生命周期状态。</summary>
        public LaunchNodeState state { get { return m_State; } }

        /// <summary>失败信息；无错误时为空字符串。</summary>
        public string error { get { return m_Error; } }

        /// <summary>节点是否已经进入终态。</summary>
        public bool isDone
        {
            get
            {
                bool isTerminal = m_State == LaunchNodeState.Succeeded
                                  || m_State == LaunchNodeState.Failed
                                  || m_State == LaunchNodeState.Stopped;
                return isTerminal;
            }
        }

        /// <summary>节点是否成功完成。</summary>
        public bool isSucceeded { get { return m_State == LaunchNodeState.Succeeded; } }

        /// <summary>
        /// 创建指定名称的启动节点。
        /// </summary>
        /// <param name="name">非空的稳定节点名称。</param>
        protected LaunchNode(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Launch node name can not be null or empty.", nameof(name));
            }

            m_Name = name;
        }

        /// <summary>从就绪状态启动节点；重复调用会被忽略。</summary>
        public void Start()
        {
            if (m_State != LaunchNodeState.Ready)
            {
                return;
            }

            m_State = LaunchNodeState.Running;

            try
            {
                OnStart();
            }
            catch (Exception exception)
            {
                Fail(exception.ToString());
            }
        }

        /// <summary>驱动运行中的节点执行一帧。</summary>
        public void DoUpdate()
        {
            if (m_State != LaunchNodeState.Running)
            {
                return;
            }

            try
            {
                OnUpdate();
            }
            catch (Exception exception)
            {
                Fail(exception.ToString());
            }
        }

        /// <summary>停止就绪或运行中的节点；终态节点保持原状态。</summary>
        public void Stop()
        {
            if (m_State == LaunchNodeState.Ready)
            {
                m_State = LaunchNodeState.Stopped;
                return;
            }

            if (m_State != LaunchNodeState.Running)
            {
                return;
            }

            try
            {
                OnStop();
            }
            catch (Exception exception)
            {
                Logger.LogException(exception);
            }

            m_State = LaunchNodeState.Stopped;
        }

        /// <summary>派生节点启动逻辑。</summary>
        protected abstract void OnStart();

        /// <summary>派生节点逐帧逻辑。</summary>
        protected virtual void OnUpdate() { }

        /// <summary>派生节点停止逻辑。</summary>
        protected virtual void OnStop() { }

        /// <summary>将运行中的节点标记为成功。</summary>
        protected void Complete()
        {
            if (m_State != LaunchNodeState.Running)
            {
                return;
            }

            m_State = LaunchNodeState.Succeeded;
        }

        /// <summary>
        /// 将运行中的节点标记为失败。
        /// </summary>
        /// <param name="error">失败信息；为空时使用默认信息。</param>
        protected void Fail(string error)
        {
            if (m_State != LaunchNodeState.Running)
            {
                return;
            }

            m_Error = string.IsNullOrEmpty(error) ? "Launch node failed." : error;
            m_State = LaunchNodeState.Failed;
        }
    }
}
```

### SequenceNode.cs

```csharp
using System;
using System.Collections.Generic;

namespace ST.Core.Launch
{
    /// <summary>
    /// 按追加顺序逐个执行子节点。
    /// </summary>
    public sealed class SequenceNode : LaunchNode
    {
        /// <summary>按执行顺序保存的子节点。</summary>
        readonly List<LaunchNode> m_Children = new List<LaunchNode>(16);

        /// <summary>当前正在处理的子节点索引。</summary>
        int m_CurrentIndex;

        /// <summary>
        /// 创建顺序节点。
        /// </summary>
        /// <param name="name">非空的稳定节点名称。</param>
        public SequenceNode(string name) : base(name) { }

        /// <summary>
        /// 追加一个尚未执行的子节点。
        /// </summary>
        /// <param name="node">非空子节点。</param>
        public void Append(LaunchNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            if (this.state != LaunchNodeState.Ready)
            {
                throw new InvalidOperationException("Can not append after sequence started.");
            }

            m_Children.Add(node);
        }

        /// <summary>启动并推进第一个子节点。</summary>
        protected override void OnStart()
        {
            Advance();
        }

        /// <summary>驱动当前子节点并继续推进。</summary>
        protected override void OnUpdate()
        {
            if (m_CurrentIndex < m_Children.Count)
            {
                m_Children[m_CurrentIndex].DoUpdate();
            }

            Advance();
        }

        /// <summary>停止当前仍在运行的子节点。</summary>
        protected override void OnStop()
        {
            if (m_CurrentIndex >= m_Children.Count)
            {
                return;
            }

            LaunchNode currentNode = m_Children[m_CurrentIndex];
            if (currentNode.state == LaunchNodeState.Running)
            {
                currentNode.Stop();
            }
        }

        /// <summary>推进到首个运行、失败或末尾节点。</summary>
        void Advance()
        {
            while (m_CurrentIndex < m_Children.Count)
            {
                LaunchNode currentNode = m_Children[m_CurrentIndex];
                if (currentNode.state == LaunchNodeState.Ready)
                {
                    currentNode.Start();
                }

                if (currentNode.state == LaunchNodeState.Running)
                {
                    return;
                }

                if (!currentNode.isSucceeded)
                {
                    Fail(currentNode.error);
                    return;
                }

                ++m_CurrentIndex;
            }

            Complete();
        }
    }
}
```

### ParallelNode.cs

```csharp
using System;
using System.Collections.Generic;

namespace ST.Core.Launch
{
    /// <summary>
    /// 同时驱动多个子节点，并在全部成功后完成。
    /// </summary>
    public sealed class ParallelNode : LaunchNode
    {
        /// <summary>并行执行的子节点。</summary>
        readonly List<LaunchNode> m_Children = new List<LaunchNode>(8);

        /// <summary>
        /// 创建并行节点。
        /// </summary>
        /// <param name="name">非空的稳定节点名称。</param>
        public ParallelNode(string name) : base(name) { }

        /// <summary>
        /// 追加一个尚未执行的子节点。
        /// </summary>
        /// <param name="node">非空子节点。</param>
        public void Append(LaunchNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            if (this.state != LaunchNodeState.Ready)
            {
                throw new InvalidOperationException("Can not append after parallel node started.");
            }

            m_Children.Add(node);
        }

        /// <summary>按追加顺序启动全部子节点。</summary>
        protected override void OnStart()
        {
            for (int i = 0; i < m_Children.Count; ++i)
            {
                LaunchNode child = m_Children[i];
                child.Start();

                if (!child.isDone || child.isSucceeded)
                {
                    continue;
                }

                StopRunningChildren();
                Fail(child.error);
                return;
            }

            CheckComplete();
        }

        /// <summary>驱动所有运行中的子节点。</summary>
        protected override void OnUpdate()
        {
            for (int i = 0; i < m_Children.Count; ++i)
            {
                if (m_Children[i].state == LaunchNodeState.Running)
                {
                    m_Children[i].DoUpdate();
                }
            }

            for (int i = 0; i < m_Children.Count; ++i)
            {
                LaunchNode child = m_Children[i];
                if (!child.isDone || child.isSucceeded)
                {
                    continue;
                }

                StopRunningChildren();
                Fail(child.error);
                return;
            }

            CheckComplete();
        }

        /// <summary>停止全部运行中的子节点。</summary>
        protected override void OnStop()
        {
            StopRunningChildren();
        }

        /// <summary>全部子节点成功时完成当前节点。</summary>
        void CheckComplete()
        {
            for (int i = 0; i < m_Children.Count; ++i)
            {
                if (!m_Children[i].isSucceeded)
                {
                    return;
                }
            }

            Complete();
        }

        /// <summary>停止全部仍在运行的子节点。</summary>
        void StopRunningChildren()
        {
            for (int i = 0; i < m_Children.Count; ++i)
            {
                if (m_Children[i].state == LaunchNodeState.Running)
                {
                    m_Children[i].Stop();
                }
            }
        }
    }
}
```

---

## Chunk 7: Exact module and application listings

### LaunchModule.cs

```csharp
using System;
using ST.Core.Logging;

namespace ST.Core.Launch
{
    /// <summary>
    /// 构建并驱动标准游戏启动节点树。
    /// </summary>
    public sealed class LaunchModule : IManager
    {
        /// <summary>标准启动根顺序节点。</summary>
        LaunchNode m_RootNode;

        /// <summary>模块当前状态。</summary>
        LaunchNodeState m_State;

        /// <summary>启动失败信息；无错误时为空字符串。</summary>
        string m_Error = string.Empty;

        /// <summary>是否已经记录根节点失败。</summary>
        bool m_IsFailureLogged;

        /// <summary>模块当前状态。</summary>
        public LaunchNodeState state { get { return m_State; } }

        /// <summary>启动失败信息；无错误时为空字符串。</summary>
        public string error { get { return m_Error; } }

        /// <summary>启动流程是否进入终态。</summary>
        public bool isDone
        {
            get
            {
                bool isTerminal = m_State == LaunchNodeState.Succeeded
                                  || m_State == LaunchNodeState.Failed
                                  || m_State == LaunchNodeState.Stopped;
                return isTerminal;
            }
        }

        /// <summary>启动流程是否成功完成。</summary>
        public bool isSucceeded { get { return m_State == LaunchNodeState.Succeeded; } }

        /// <summary>IManager 初始化入口；节点树由 Launch 创建。</summary>
        public override void DoInit() { }

        /// <summary>驱动运行中的启动节点树。</summary>
        public override void DoUpdate()
        {
            if (m_State != LaunchNodeState.Running || m_RootNode == null)
            {
                return;
            }

            m_RootNode.DoUpdate();
            SyncRootState();
        }

        /// <summary>启动树没有滞后帧任务。</summary>
        public override void DoLateUpdate() { }

        /// <summary>停止尚未完成的启动节点树。</summary>
        public override void DoClose()
        {
            if (m_State == LaunchNodeState.Ready)
            {
                m_State = LaunchNodeState.Stopped;
                return;
            }

            if (m_State != LaunchNodeState.Running)
            {
                return;
            }

            if (m_RootNode != null)
            {
                m_RootNode.Stop();
            }

            m_State = LaunchNodeState.Stopped;
        }

        /// <summary>构建并启动标准节点树；重复调用会被忽略。</summary>
        public void Launch()
        {
            if (m_State != LaunchNodeState.Ready)
            {
                return;
            }

            try
            {
                m_RootNode = CreateRootNode();
                m_State = LaunchNodeState.Running;
                m_RootNode.Start();
                SyncRootState();
            }
            catch (Exception exception)
            {
                m_Error = exception.ToString();
                m_State = LaunchNodeState.Failed;
                m_IsFailureLogged = true;
                Logger.LogException(exception);
            }
        }

        /// <summary>创建标准根节点树。</summary>
        SequenceNode CreateRootNode()
        {
            SequenceNode rootNode = new SequenceNode("LaunchNode");
            rootNode.Append(CreateNode(LaunchNodeType.WaitFrame));
            rootNode.Append(CreateNode(LaunchNodeType.AppInit));

            ParallelNode splashNode = new ParallelNode("Splash");
            splashNode.Append(CreateNode(LaunchNodeType.OpenLaunchShell));
            splashNode.Append(CreateNode(LaunchNodeType.SplashGlobalResLoad));
            splashNode.Append(CreateNode(LaunchNodeType.DownloadServerConfig));
            splashNode.Append(CreateNode(LaunchNodeType.TableLoad));
            rootNode.Append(splashNode);

            rootNode.Append(CreateNode(LaunchNodeType.CheckDownloadServerConfigSuccess));
            rootNode.Append(CreateNode(LaunchNodeType.BeforeLoginGlobalResLoad));
            rootNode.Append(CreateNode(LaunchNodeType.DownloadServerDynamic));
            rootNode.Append(CreateNode(LaunchNodeType.CheckDownloadServerDynamicSuccess));
            rootNode.Append(CreateNode(LaunchNodeType.SDKInit));
            rootNode.Append(CreateNode(LaunchNodeType.SDKInitCheck));
            rootNode.Append(CreateNode(LaunchNodeType.CheckVersion));
            rootNode.Append(CreateNode(LaunchNodeType.Update));
            rootNode.Append(CreateNode(LaunchNodeType.ReloadRes));
            rootNode.Append(CreateNode(LaunchNodeType.CompileShader));
            rootNode.Append(CreateNode(LaunchNodeType.CheckSubpackUpdate));
            rootNode.Append(CreateNode(LaunchNodeType.LaunchGC));
            rootNode.Append(CreateNode(LaunchNodeType.OpenLoginPanel));
            rootNode.Append(CreateNode(LaunchNodeType.SDKLogin));
            rootNode.Append(CreateNode(LaunchNodeType.End));
            return rootNode;
        }

        /// <summary>
        /// 创建指定类型的具体启动节点。
        /// </summary>
        /// <param name="type">有效的标准启动节点类型。</param>
        /// <returns>与类型对应的新节点。</returns>
        LaunchNode CreateNode(LaunchNodeType type)
        {
            switch (type)
            {
                case LaunchNodeType.WaitFrame:
                    return new WaitFrameNode();
                case LaunchNodeType.AppInit:
                    return new AppInitNode();
                case LaunchNodeType.OpenLaunchShell:
                    return new OpenLaunchShellNode();
                case LaunchNodeType.SplashGlobalResLoad:
                    return new SplashGlobalResLoadNode();
                case LaunchNodeType.DownloadServerConfig:
                    return new DownloadServerConfigNode();
                case LaunchNodeType.TableLoad:
                    return new TableLoadNode();
                case LaunchNodeType.CheckDownloadServerConfigSuccess:
                    return new CheckDownloadServerConfigSuccessNode();
                case LaunchNodeType.BeforeLoginGlobalResLoad:
                    return new BeforeLoginGlobalResLoadNode();
                case LaunchNodeType.DownloadServerDynamic:
                    return new DownloadServerDynamicNode();
                case LaunchNodeType.CheckDownloadServerDynamicSuccess:
                    return new CheckDownloadServerDynamicSuccessNode();
                case LaunchNodeType.SDKInit:
                    return new SDKInitNode();
                case LaunchNodeType.SDKInitCheck:
                    return new SDKInitCheckNode();
                case LaunchNodeType.CheckVersion:
                    return new CheckVersionNode();
                case LaunchNodeType.Update:
                    return new UpdateNode();
                case LaunchNodeType.ReloadRes:
                    return new ReloadResNode();
                case LaunchNodeType.CompileShader:
                    return new CompileShaderNode();
                case LaunchNodeType.CheckSubpackUpdate:
                    return new CheckSubpackUpdateNode();
                case LaunchNodeType.LaunchGC:
                    return new LaunchGCNode();
                case LaunchNodeType.OpenLoginPanel:
                    return new OpenLoginPanelNode();
                case LaunchNodeType.SDKLogin:
                    return new SDKLoginNode();
                case LaunchNodeType.End:
                    return new EndNode();
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown launch node type.");
            }
        }

        /// <summary>同步根节点终态，并只记录一次失败。</summary>
        void SyncRootState()
        {
            if (m_RootNode == null || !m_RootNode.isDone)
            {
                return;
            }

            m_State = m_RootNode.state;
            m_Error = m_RootNode.error;

            if (m_State != LaunchNodeState.Failed || m_IsFailureLogged)
            {
                return;
            }

            m_IsFailureLogged = true;
            Logger.LogError(m_Error);
        }
    }
}
```

### GameApplication.cs

```csharp
namespace ST.Core.Launch
{
    /// <summary>
    /// 游戏运行入口，负责持有并驱动启动模块。
    /// </summary>
    public sealed class GameApplication : IManager
    {
        /// <summary>标准启动模块。</summary>
        LaunchModule m_LaunchModule;

        /// <summary>是否已经初始化。</summary>
        bool m_IsInitialized;

        /// <summary>是否已经关闭。</summary>
        bool m_IsClosed;

        /// <summary>标准启动模块；初始化前为空。</summary>
        public LaunchModule launchModule { get { return m_LaunchModule; } }

        /// <summary>创建并启动唯一的启动模块。</summary>
        public override void DoInit()
        {
            if (m_IsInitialized || m_IsClosed)
            {
                return;
            }

            m_LaunchModule = new LaunchModule();
            m_LaunchModule.DoInit();
            m_LaunchModule.Launch();
            m_IsInitialized = true;
        }

        /// <summary>驱动启动模块的逐帧逻辑。</summary>
        public override void DoUpdate()
        {
            if (!m_IsInitialized || m_IsClosed)
            {
                return;
            }

            m_LaunchModule.DoUpdate();
        }

        /// <summary>驱动启动模块的滞后帧逻辑。</summary>
        public override void DoLateUpdate()
        {
            if (!m_IsInitialized || m_IsClosed)
            {
                return;
            }

            m_LaunchModule.DoLateUpdate();
        }

        /// <summary>关闭启动模块；重复调用会被忽略。</summary>
        public override void DoClose()
        {
            if (m_IsClosed)
            {
                return;
            }

            m_IsClosed = true;
            if (m_LaunchModule == null)
            {
                return;
            }

            m_LaunchModule.DoClose();
        }
    }
}
```

### GameLauncher.cs

```csharp
using UnityEngine;

namespace ST.Core.Launch
{
    /// <summary>
    /// 启动场景组件，将 Unity 生命周期转发给游戏运行入口。
    /// </summary>
    public sealed class GameLauncher : MonoBehaviour
    {
        /// <summary>游戏运行入口。</summary>
        GameApplication m_GameApplication;

        /// <summary>创建并初始化游戏运行入口。</summary>
        void Awake()
        {
            m_GameApplication = new GameApplication();
            m_GameApplication.DoInit();
        }

        /// <summary>转发 Unity 逐帧更新。</summary>
        void Update()
        {
            if (m_GameApplication == null)
            {
                return;
            }

            m_GameApplication.DoUpdate();
        }

        /// <summary>转发 Unity 滞后帧更新。</summary>
        void LateUpdate()
        {
            if (m_GameApplication == null)
            {
                return;
            }

            m_GameApplication.DoLateUpdate();
        }

        /// <summary>销毁时关闭游戏运行入口。</summary>
        void OnDestroy()
        {
            if (m_GameApplication == null)
            {
                return;
            }

            m_GameApplication.DoClose();
            m_GameApplication = null;
        }
    }
}
```
