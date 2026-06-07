[Exception] ArgumentException: System.ArgumentException: System.String Unity.Entities.EntityComponentStore::AppendRemovedComponentRecordError(Unity.Entities.Entity,Unity.Entities.ComponentType)
This Exception was thrown from a function compiled with Burst, which has limited exception support.
0x00007ffc04c27e4b (Unity) burst_abort
0x00007ffbf8c57d1e (1ca0df14f5d362976563afc136dafea) burst_Abort_Trampoline
0x00007ffbf8bd1c5e (1ca0df14f5d362976563afc136dafea) Unity.Entities.EntityComponentStore.AssertEntityHasComponent (at C:/UnityProjects/Bust and Build/Library/PackageCache/com.unity.burst@1df634d836b8/.Runtime/Library/PackageCache/com.unity.entities@f6e02210e263/Unity.Entities/EntityComponentStoreDebug.cs:342)
0x00007ffbf8be45dc (1ca0df14f5d362976563afc136dafea) Unity.Entities.EntityCommandBuffer.EcbWalker`1<Unity.Entities.EntityCommandBuffer.PlaybackProcessor>.ProcessChain (at C:/UnityProjects/Bust and Build/Library/PackageCache/com.unity.burst@1df634d836b8/.Runtime/Library/PackageCache/com.unity.entities@f6e02210e263/Unity.Entities/EntityCommandBuffer.cs:4002)
0x00007ffbf8bdafeb (1ca0df14f5d362976563afc136dafea) 6cbe9b44f55dff161edecf05c9660a36
0x00000296f44ea438 (Mono JIT Code) (wrapper managed-to-native) object:wrapper_native_00000296574E0240 (intptr,int,intptr,int,int)
0x0000029702e4ba14 (Mono JIT Code) (wrapper delegate-invoke) <Module>:invoke_void_intptr_int_intptr_int_int (intptr,int,intptr,int,int)
0x0000029702e4b846 (Mono JIT Code) Unity.Entities.ECBInterop:_forward_mono_ProcessChainChunk (void*,int,Unity.Entities.ECBChainPlaybackState*,int,int) (at ./Library/PackageCache/com.unity.entities@f6e02210e263/Unity.Entities/ECBInterop.interop.gen.cs:107)
0x0000029702e4b623 (Mono JIT Code) Unity.Entities.ECBInterop:ProcessChainChunk (void*,int,Unity.Entities.ECBChainPlaybackState*,int,int) (at ./Library/PackageCache/com.unity.entities@f6e02210e263/Unity.Entities/ECBInterop.interop.gen.cs:90)
0x0000029702e49ee3 (Mono JIT Code) Unity.Entities.EntityCommandBuffer/EcbWalker`1<Unity.Entities.EntityCommandBuffer/PlaybackProcessor>:WalkChains (Unity.Entities.EntityCommandBuffer) (at ./Library/PackageCache/com.unity.entities@f6e02210e263/Unity.Entities/EntityCommandBuffer.cs:3952)
0x0000029702e47d8b (Mono JIT Code) Unity.Entities.EntityCommandBuffer:PlaybackInternal (Unity.Entities.EntityDataAccess*) (at ./Library/PackageCache/com.unity.entities@f6e02210e263/Unity.Entities/EntityCommandBuffer.cs:3738)
0x0000029702e46a73 (Mono JIT Code) Unity.Entities.EntityCommandBuffer:Playback (Unity.Entities.EntityManager) (at ./Library/PackageCache/com.unity.entities@f6e02210e263/Unity.Entities/EntityCommandBuffer.cs:3678)
0x00000296f57a09c3 (Mono JIT Code) Unity.Entities.EntityCommandBufferSystem:FlushPendingBuffers (bool) (at ./Library/PackageCache/com.unity.entities@f6e02210e263/Unity.Entities/EntityCommandBufferSystem.cs:217)
0x00000296f579f5db (Mono JIT Code) Unity.Entities.EntityCommandBufferSystem:OnUpdate () (at ./Library/PackageCache/com.unity.entities@f6e02210e263/Unity.Entities/EntityCommandBufferSystem.cs:182)
0x00000296f579c65f (Mono JIT Code) Unity.Entities.SystemBase:Update () (at ./Library/PackageCache/com.unity.entities@f6e02210e263/Unity.Entities/SystemBase.cs:420)
0x00000296f579f036 (Mono JIT Code) Unity.Entities.ComponentSystemGroup:UpdateAllSystems () (at ./Library/PackageCache/com.unity.entities@f6e02210e263/Unity.Entities/ComponentSystemGroup.cs:723)
0x00000296f579e69b (Mono JIT Code) Unity.Entities.ComponentSystemGroup:OnUpdate () (at ./Library/PackageCache/com.unity.entities@f6e02210e263/Unity.Entities/ComponentSystemGroup.cs:681)
0x00000296f579c65f (Mono JIT Code) Unity.Entities.SystemBase:Update () (at ./Library/PackageCache/com.unity.entities@f6e02210e263/Unity.Entities/SystemBase.cs:420)
0x00000296f579bf04 (Mono JIT Code) Unity.Entities.ScriptBehaviourUpdateOrder/DummyDelegateWrapper:TriggerUpdate () (at ./Library/PackageCache/com.unity.entities@f6e02210e263/Unity.Entities/ScriptBehaviourUpdateOrder.cs:523)
0x00000296f6ad1028 (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)
0x00007ffbff636e7e (mono-2.0-bdwgc) mono_jit_runtime_invoke (at C:/build/output/Unity-Technologies/mono/mono/mini/mini-runtime.c:3445)
0x00007ffbff578874 (mono-2.0-bdwgc) do_runtime_invoke (at C:/build/output/Unity-Technologies/mono/mono/metadata/object.c:3068)
0x00007ffbff578960 (mono-2.0-bdwgc) mono_runtime_invoke (at C:/build/output/Unity-Technologies/mono/mono/metadata/object.c:3115)
0x00007ffc0500bf24 (Unity) scripting_method_invoke
0x00007ffc04fe44c3 (Unity) ScriptingInvocation::Invoke
0x00007ffc04c70564 (Unity) ExecutePlayerLoop
0x00007ffc04c70585 (Unity) ExecutePlayerLoop
0x00007ffc04c76a3f (Unity) PlayerLoop
0x00007ffc05d8c56a (Unity) EditorPlayerLoop::Execute
0x00007ffc05da320f (Unity) PlayerLoopController::InternalUpdateScene
0x00007ffc05da4fdf (Unity) PlayerLoopController::UpdateSceneIfNeededFromMainLoop
0x00007ffc05d9ed92 (Unity) Application::TickTimer
0x00007ffc0636ff6c (Unity) MainMessageLoop
0x00007ffc063776dc (Unity) UnityMain
0x00007ff645592f2a (Unity) __scrt_common_main_seh
0x00007ffcdace7374 (KERNE
EntityCommandBuffer was recorded in PearlSpawnOnDeathSystem and played back in Unity.Entities.EndSimulationEntityCommandBufferSystem.
  at (wrapper managed-to-native) System.Object.wrapper_native_00000296574E0240(intptr,int,intptr,int,int)
  at (wrapper delegate-invoke) <Module>.invoke_void_intptr_int_intptr_int_int(intptr,int,intptr,int,int)
  at Unity.Entities.ECBInterop._forward_mono_ProcessChainChunk (System.Void* walker, System.Int32 processorType, Unity.Entities.ECBChainPlaybackState* chainStates, System.Int32 currentChain, System.Int32 nextChain) [0x00001] in .\Library\PackageCache\com.unity.entities@f6e02210e263\Unity.Entities\ECBInterop.interop.gen.cs:107 
  at Unity.Entities.ECBInterop.ProcessChainChunk (System.Void* walker, System.Int32 processorType, Unity.Entities.ECBChainPlaybackState* chainStates, System.Int32 currentChain, System.Int32 nextChain) [0x0000b] in .\Library\PackageCache\com.unity.entities@f6e02210e263\Unity.Entities\ECBInterop.interop.gen.cs:90 
  at Unity.Entities.EntityCommandBuffer+EcbWalker`1[T].WalkChains (Unity.Entities.EntityCommandBuffer ecb) [0x002d6] in .\Library\PackageCache\com.unity.entities@f6e02210e263\Unity.Entities\EntityCommandBuffer.cs:3952 
  at Unity.Entities.EntityCommandBuffer.PlaybackInternal (Unity.Entities.EntityDataAccess* mgr) [0x001a5] in .\Library\PackageCache\com.unity.entities@f6e02210e263\Unity.Entities\EntityCommandBuffer.cs:3738 
  at Unity.Entities.EntityCommandBuffer.Playback (Unity.Entities.EntityManager mgr) [0x00001] in .\Library\PackageCache\com.unity.entities@f6e02210e263\Unity.Entities\EntityCommandBuffer.cs:3678 
  at Unity.Entities.EntityCommandBufferSystem.FlushPendingBuffers (System.Boolean playBack) [0x000d1] in .\Library\PackageCache\com.unity.entities@f6e02210e263\Unity.Entities\EntityCommandBufferSystem.cs:217 
Assertion failure. Value was False
Expected: True
EntityCommandBuffer was recorded in DeathSystem and played back in Unity.Entities.EndSimulationEntityCommandBufferSystem.
  at UnityEngine.Assertions.Assert.Fail (System.String message, System.String userMessage) [0x00043] in <038ae34513ab4db49caf29025be8f045>:0 
  at UnityEngine.Assertions.Assert.IsTrue (System.Boolean condition, System.String message) [0x0000f] in <038ae34513ab4db49caf29025be8f045>:0 
  at UnityEngine.Assertions.Assert.IsTrue (System.Boolean condition) [0x00009] in <038ae34513ab4db49caf29025be8f045>:0 
  at Unity.Assertions.Assert.IsTrue (System.Boolean condition) [0x00008] in .\Library\PackageCache\com.unity.entities@f6e02210e263\Unity.Entities\Stubs\Unity.Assertions\Assert.cs:24 
  at Unity.Entities.EntityComponentStore.AssertNoQueuedManagedDeferredCommands () [0x00019] in .\Library\PackageCache\com.unity.entities@f6e02210e263\Unity.Entities\EntityComponentStore.cs:3019 
  at Unity.Entities.EntityDataAccess.BeforeStructuralChange () [0x00008] in .\Library\PackageCache\com.unity.entities@f6e02210e263\Unity.Entities\EntityDataAccess.cs:411 
  at Unity.Entities.EntityDataAccess.BeginStructuralChanges () [0x0000e] in .\Library\PackageCache\com.unity.entities@f6e02210e263\Unity.Entities\EntityDataAccess.cs:420 
  at Unity.Entities.EntityCommandBuffer+PlaybackProcessor.Init (Unity.Entities.EntityDataAccess* entityDataAccess, Unity.Entities.EntityCommandBufferData* data, Unity.Entities.SystemHandle& originSystemHandle) [0x00041] in .\Library\PackageCache\com.unity.entities@f6e02210e263\Unity.Entities\EntityCommandBuffer.cs:4189 
  at Unity.Entities.EntityCommandBuffer.PlaybackInternal (Unity.Entities.EntityDataAccess* mgr) [0x00179] in .\Library\PackageCache\com.unity.entities@f6e02210e263\Unity.Entities\EntityCommandBuffer.cs:3734 
  at Unity.Entities.EntityCommandBuffer.Playback (Unity.Entities.EntityManager mgr) [0x00001] in .\Library\PackageCache\com.unity.entities@f6e02210e263\Unity.Entities\EntityCommandBuffer.cs:3678 
  at Unity.Entities.EntityCommandBufferSystem.FlushPendingBuffers (System.Boolean playBack) [0x000d1] in .\Library\PackageCache\com.unity.entities@f6e02210e263\Unity.Entities\EntityCommandBufferSystem.cs:217 

EntityCommandBufferSystem.FlushPendingBuffers() at ./Library/PackageCache/com.unity.entities@f6e02210e263/Unity.Entities/EntityCommandBufferSystem.cs:280
 278:                      exceptionMessage.AppendLine(err);
 279:                  }
--> 280:                  throw new ArgumentException(exceptionMessage.ToString());
 281:              }
 282:  #endif

EntityCommandBufferSystem.OnUpdate() at ./Library/PackageCache/com.unity.entities@f6e02210e263/Unity.Entities/EntityCommandBufferSystem.cs:182
 180:  protected override void OnUpdate()
 181:  {
--> 182:      FlushPendingBuffers(true);
 183:      PendingBuffers.Clear();
 184:  }

SystemBase.Update() at ./Library/PackageCache/com.unity.entities@f6e02210e263/Unity.Entities/SystemBase.cs:420
 418:                          }
--> 420:                          OnUpdate();
 421:  #if ENABLE_UNITY_COLLECTIONS_CHECKS
 422:                          success = true;

ComponentSystemGroup.UpdateAllSystems() at ./Library/PackageCache/com.unity.entities@f6e02210e263/Unity.Entities/ComponentSystemGroup.cs:723
 721:          // Update managed code.
 722:          var sys = m_managedSystemsToUpdate[index.Index];
--> 723:          sys.Update();
 724:      }
 725:  }

Debug.LogException()

Debug.LogException() at ./Library/PackageCache/com.unity.entities@f6e02210e263/Unity.Entities/Stubs/Unity/Debug.cs:17
  15:      UnityEngine.Debug.Log(message);
  16:  public static void LogException(Exception exception) =>
-->  17:      UnityEngine.Debug.LogException(exception);
  19:  public static void LogError(object message, UnityObject context) =>

ComponentSystemGroup.UpdateAllSystems() at ./Library/PackageCache/com.unity.entities@f6e02210e263/Unity.Entities/ComponentSystemGroup.cs:728
 726:  catch (Exception e)
 727:  {
--> 728:      Debug.LogException(e);
 729:  }

ComponentSystemGroup.OnUpdate() at ./Library/PackageCache/com.unity.entities@f6e02210e263/Unity.Entities/ComponentSystemGroup.cs:681
 679:  if (RateManager == null)
 680:  {
--> 681:      UpdateAllSystems();
 682:  }
 683:  else

SystemBase.Update() at ./Library/PackageCache/com.unity.entities@f6e02210e263/Unity.Entities/SystemBase.cs:420
 418:                          }
--> 420:                          OnUpdate();
 421:  #if ENABLE_UNITY_COLLECTIONS_CHECKS
 422:                          success = true;

Unity.Entities.DummyDelegateWrapper.TriggerUpdate() at ./Library/PackageCache/com.unity.entities@f6e02210e263/Unity.Entities/ScriptBehaviourUpdateOrder.cs:523
 521:      if (m_System.m_StatePtr != null)
 522:      {
--> 523:          m_System.Update();
 524:      }
 525:  }
