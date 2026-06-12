[Exception] ArgumentException: System.ArgumentException: An EntityManager command is operating on an invalid entity. This usually means that the Entity has already been destroyed or was never created. This can happen when a subscene is opened or closed between when a EntityCommandBuffer has been recorded and played back. This can invalidate recorded Entities.System.String Unity.Entities.EntityComponentStore::AppendDestroyedEntityRecordError(Unity.Entities.Entity)
This Exception was thrown from a function compiled with Burst, which has limited exception support.
0x00007ffc04c27e4b (Unity) burst_abort
0x00007ffbfaa97d1e (1ca0df14f5d362976563afc136dafea) burst_Abort_Trampoline
0x00007ffbfaa119bc (1ca0df14f5d362976563afc136dafea) Unity.Entities.EntityComponentStore.AssertEntitiesExist (at C:/UnityProjects/Bust and Build/Library/PackageCache/com.unity.burst@1df634d836b8/.Runtime/Library/PackageCache/com.unity.entities@f6e02210e263/Unity.Entities/EntityComponentStoreDebug.cs:315)
0x00007ffbfaa12f86 (1ca0df14f5d362976563afc136dafea) Unity.Entities.EntityDataAccess.InstantiateInternalDuringStructuralChange (at C:/UnityProjects/Bust and Build/Library/PackageCache/com.unity.burst@1df634d836b8/.Runtime/Library/PackageCache/com.unity.entities@f6e02210e263/Unity.Entities/EntityDataAccess.cs:2760)
0x00007ffbfaa24681 (1ca0df14f5d362976563afc136dafea) Unity.Entities.EntityCommandBuffer.EcbWalker`1<Unity.Entities.EntityCommandBuffer.PlaybackProcessor>.ProcessChain (at C:/UnityProjects/Bust and Build/Library/PackageCache/com.unity.burst@1df634d836b8/.Runtime/Library/PackageCache/com.unity.entities@f6e02210e263/Unity.Entities/EntityCommandBuffer.cs:4002)
0x00007ffbfaa1afeb (1ca0df14f5d362976563afc136dafea) 6cbe9b44f55dff161edecf05c9660a36
0x0000019502cf8ab8 (Mono JIT Code) (wrapper managed-to-native) object:wrapper_native_000001951D9F0240 (intptr,int,intptr,int,int)
0x000001950589f464 (Mono JIT Code) (wrapper delegate-invoke) <Module>:invoke_void_intptr_int_intptr_int_int (intptr,int,intptr,int,int)
0x000001950589f296 (Mono JIT Code) Unity.Entities.ECBInterop:_forward_mono_ProcessChainChunk (void*,int,Unity.Entities.ECBChainPlaybackState*,int,int) (at ./Library/PackageCache/com.unity.entities@f6e02210e263/Unity.Entities/ECBInterop.interop.gen.cs:107)
0x000001950589f073 (Mono JIT Code) Unity.Entities.ECBInterop:ProcessChainChunk (void*,int,Unity.Entities.ECBChainPlaybackState*,int,int) (at ./Library/PackageCache/com.unity.entities@f6e02210e263/Unity.Entities/ECBInterop.interop.gen.cs:90)
0x000001950589d933 (Mono JIT Code) Unity.Entities.EntityCommandBuffer/EcbWalker`1<Unity.Entities.EntityCommandBuffer/PlaybackProcessor>:WalkChains (Unity.Entities.EntityCommandBuffer) (at ./Library/PackageCache/com.unity.entities@f6e02210e263/Unity.Entities/EntityCommandBuffer.cs:3952)
0x000001950589b7db (Mono JIT Code) Unity.Entities.EntityCommandBuffer:PlaybackInternal (Unity.Entities.EntityDataAccess*) (at ./Library/PackageCache/com.unity.entities@f6e02210e263/Unity.Entities/EntityCommandBuffer.cs:3738)
0x000001950589a4c3 (Mono JIT Code) Unity.Entities.EntityCommandBuffer:Playback (Unity.Entities.EntityManager) (at ./Library/PackageCache/com.unity.entities@f6e02210e263/Unity.Entities/EntityCommandBuffer.cs:3678)
0x000001964b929223 (Mono JIT Code) Unity.Entities.EntityCommandBufferSystem:FlushPendingBuffers (bool) (at ./Library/PackageCache/com.unity.entities@f6e02210e263/Unity.Entities/EntityCommandBufferSystem.cs:217)
0x000001964b92874b (Mono JIT Code) Unity.Entities.EntityCommandBufferSystem:OnUpdate () (at ./Library/PackageCache/com.unity.entities@f6e02210e263/Unity.Entities/EntityCommandBufferSystem.cs:182)
0x000001964b9257cf (Mono JIT Code) Unity.Entities.SystemBase:Update () (at ./Library/PackageCache/com.unity.entities@f6e02210e263/Unity.Entities/SystemBase.cs:420)
0x000001964b9281a6 (Mono JIT Code) Unity.Entities.ComponentSystemGroup:UpdateAllSystems () (at ./Library/PackageCache/com.unity.entities@f6e02210e263/Unity.Entities/ComponentSystemGroup.cs:723)
0x000001964b92780b (Mono JIT Code) Unity.Entities.ComponentSystemGroup:OnUpdate () (at ./Library/PackageCache/com.unity.entities@f6e02210e263/Unity.Entities/ComponentSystemGroup.cs:681)
0x000001964b9257cf (Mono JIT Code) Unity.Entities.SystemBase:Update () (at ./Library/PackageCache/com.unity.entities@f6e02210e263/Unity.Entities/SystemBase.cs:420)
0x000001964b925074 (Mono JIT Code) Unity.Entities.ScriptBehaviourUpdateOrder/DummyDelegateWrapper:TriggerUpdate () (at ./Library/PackageCache/com.unity.entities@f6e02210e263/Unity.Entities/ScriptBehaviourUpdateOrder.cs:523)
0x000001945b97eab8 (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)
0x00007ffbffbf6e7e (mono-2.0-bdwgc) mono_jit_runtime_invoke (at C:/build/output/Unity-Technologies/mono/mono/mini/mini-runtime.c:3445)
0x00007ffbffb38874 (mono-2.0-bdwgc) do_runtime_invoke (at C:/build/output/Unity-Technologies/mono/mono/metadata/object.c:3068)
0x00007ffbffb38960 (mono-2.0-bdwgc) mono_runtime_invoke (at C:/build/output/Unity-Technologies/mono/mono/metadata/object.c:3115)
0x00007ffc0500bf24 (Unity) scripting_method_invoke
0x0000
EntityCommandBuffer was recorded in PickupSpawnOnDeathSystem and played back in Unity.Entities.EndSimulationEntityCommandBufferSystem.
  at (wrapper managed-to-native) System.Object.wrapper_native_000001951D9F0240(intptr,int,intptr,int,int)
  at (wrapper delegate-invoke) <Module>.invoke_void_intptr_int_intptr_int_int(intptr,int,intptr,int,int)
  at Unity.Entities.ECBInterop._forward_mono_ProcessChainChunk (System.Void* walker, System.Int32 processorType, Unity.Entities.ECBChainPlaybackState* chainStates, System.Int32 currentChain, System.Int32 nextChain) [0x00001] in .\Library\PackageCache\com.unity.entities@f6e02210e263\Unity.Entities\ECBInterop.interop.gen.cs:107 
  at Unity.Entities.ECBInterop.ProcessChainChunk (System.Void* walker, System.Int32 processorType, Unity.Entities.ECBChainPlaybackState* chainStates, System.Int32 currentChain, System.Int32 nextChain) [0x0000b] in .\Library\PackageCache\com.unity.entities@f6e02210e263\Unity.Entities\ECBInterop.interop.gen.cs:90 
  at Unity.Entities.EntityCommandBuffer+EcbWalker`1[T].WalkChains (Unity.Entities.EntityCommandBuffer ecb) [0x002d6] in .\Library\PackageCache\com.unity.entities@f6e02210e263\Unity.Entities\EntityCommandBuffer.cs:3952 
  at Unity.Entities.EntityCommandBuffer.PlaybackInternal (Unity.Entities.EntityDataAccess* mgr) [0x001a5] in .\Library\PackageCache\com.unity.entities@f6e02210e263\Unity.Entities\EntityCommandBuffer.cs:3738 
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
