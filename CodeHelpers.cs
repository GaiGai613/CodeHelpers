﻿using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using CodeHelpers.Collections;
using CodeHelpers.ThreadHelpers;
using UnityEngine;

//REMEMBER that we need to make CodeHelpers.CodeHelperMonoBehaviour execute before other scripts by placing it in the 
//project settings, or some methods in CodeHelpers.InputHelpers and CodeHelpers.CodeHelperMonoBehaviour.PreUpdateMethods
//would not work.

namespace CodeHelpers
{
	public static class CodeHelper
	{

#region Special Method Helpers

		/// <summary>This will generate a method that can be only called once a frame. They will be called at end frame. (Thread safe)</summary>
		public static Action GetOneCallMethod(Action action) => new OneCallMethod(action).CallAction;

		class OneCallMethod
		{
			public OneCallMethod(Action action)
			{
				invokeAction = () =>
							   {
								   action();
								   Interlocked.Exchange(ref hasCalled, 0);
							   };
			}

			readonly Action invokeAction;
			int hasCalled;

			public void CallAction()
			{
				if (Interlocked.Exchange(ref hasCalled, 1) == 1) return;
				InvokeEndFrame(invokeAction);
			}
		}

		/// <summary>This will generate a method that will invoke a method randomly within a period of time. (Not thread safe)</summary>
		public static Action GetRandomInvokeMethod(Action action, float maxDelay = 1f) => new RandomInvokeMethod(maxDelay, action).CallAction;

		class RandomInvokeMethod
		{
			public RandomInvokeMethod(float delay, Action action)
			{
				this.delay = delay;
				this.action = action;
			}

			readonly float delay;
			readonly Action action;

			bool isCalling;

			public void CallAction()
			{
				if (isCalling) return;

				isCalling = true;
				StartCoroutine(RandomInvoke());
			}

			IEnumerator RandomInvoke()
			{
				float thisDelay = (float)RandomHelper.Value * delay;

				yield return new WaitForSeconds(thisDelay);

				action();
				isCalling = false;
			}
		}

#endregion

#region Call Events

		internal static readonly InvokeMethod invokeBeforeFrame = new InvokeMethod();
		internal static readonly InvokeMethod invokeNextFrame = new InvokeMethod();
		internal static readonly InvokeMethod invokeEndFrame = new InvokeMethod();

		//These methods does not ensure which frame the action execute, but it will execute them in the correct frame phase

		public static void InvokeBeforeFrame(Action action) => invokeBeforeFrame.Add(action);
		public static void InvokeNextFrame(Action action) => invokeNextFrame.Add(action);
		public static void InvokeEndFrame(Action action) => invokeEndFrame.Add(action);

		public static void InvokeAfter(Action action, float delay) //A great tool for debugging
		{
			if (action == null) throw new NullReferenceException("action cannot be null!!!");
			StartCoroutine(CallMethodAfterCoroutine(action, delay));
		}

		static IEnumerator CallMethodAfterCoroutine(Action action, float delay)
		{
			yield return new WaitForSeconds(delay);
			action();
		}

		internal class InvokeMethod
		{
			readonly Queue<Action> actions = new Queue<Action>();
			readonly ConcurrentQueue<Action> threadedActions = new ConcurrentQueue<Action>();

			public void Add(Action action)
			{
				if (action == null) throw new NullReferenceException("action cannot be null!");

				if (ThreadHelper.IsOnMainThread) actions.Enqueue(action);
				else threadedActions.Enqueue(action);
			}

			public void InvokeAll()
			{
				while (actions.Count != 0) actions.Dequeue()();

				while (!threadedActions.IsEmpty)
				{
					if (threadedActions.TryDequeue(out Action action)) action();
				}
			}
		}

#endregion

		public static Coroutine StartCoroutine(IEnumerator coroutine)
		{
			if (CodeHelperMonoBehavior.instance == null) throw new Exception("You did not add CodeHelper in the scene yet!!!");
			return CodeHelperMonoBehavior.instance.StartCoroutine(coroutine);
		}

		public static void StopCoroutine(IEnumerator coroutine)
		{
			if (CodeHelperMonoBehavior.instance == null) throw new Exception("You did not add CodeHelper in the scene yet!!!");
			CodeHelperMonoBehavior.instance.StopCoroutine(coroutine);
		}

		public static void StopCoroutine(Coroutine coroutine)
		{
			if (CodeHelperMonoBehavior.instance == null) throw new Exception("You did not add CodeHelper in the scene yet!!!");
			CodeHelperMonoBehavior.instance.StopCoroutine(coroutine);
		}

		public static void Swap<T>(ref T first, ref T second)
		{
			T storage = first;
			first = second;
			second = storage;
		}

		public static T ToNotNullable<T>(this T? nullable, out bool isNull) where T : struct
		{
			isNull = nullable == null;
			return nullable.GetValueOrDefault();
		}

		public static T GetComponent<T>(this Behaviour behaviour, ref T cache)
		{
			if (cache != null) return cache;
			return cache = behaviour.GetComponent<T>();
		}

		public static T GetComponentInChildren<T>(this Behaviour behaviour, ref T cache)
		{
			if (cache != null) return cache;
			return cache = behaviour.GetComponentInChildren<T>();
		}

		public static T GetComponentInParent<T>(this Behaviour behaviour, ref T cache)
		{
			if (cache != null) return cache;
			return cache = behaviour.GetComponentInParent<T>();
		}

		public static float GetHorizontalFieldOfView(this Camera camera)
		{
			var tan = Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad / 2f);
			return Mathf.Rad2Deg * 2f * Mathf.Atan(tan * camera.aspect);
		}

		public static string ToMethodSignature(this MethodInfo method) =>
			$@"{method.ReturnType.Name} {method.Name}({string.Join(", ", from parameter in method.GetParameters()
																		 select $"{parameter.ParameterType.Name} {parameter.Name}")})";

		public static string ToMethodSignatureNoReturnType(this MethodInfo method) =>
			$@"{method.Name}({string.Join(", ", from parameter in method.GetParameters()
												select $"{parameter.ParameterType.Name} {parameter.Name}")})";

		public static readonly object placeholder = new object();
	}
}