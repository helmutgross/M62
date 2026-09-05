using System;
using System.Reflection;

namespace M62Logic
{
	// Token: 0x02000004 RID: 4
	public class FastPort
	{
		// Token: 0x06000003 RID: 3 RVA: 0x0000214A File Offset: 0x0000034A
		public FastPort(object tgt, PropertyInfo p, MethodInfo um)
		{
			this.target = tgt;
			this.prop = p;
			this.updateMethod = um;
			this.BuildFastAccessors();
		}

		// Builds bound (closed-instance) delegates for the common case (float property / float-arg method)
		// to avoid per-call reflection boxing on the hot path (called many times per frame).
		// Falls back silently to plain reflection (original behaviour) if anything is incompatible.
		private void BuildFastAccessors()
		{
			try
			{
				bool canFastGet = this.target != null && this.prop != null && this.prop.CanRead && this.prop.PropertyType == typeof(float);
				if (canFastGet)
				{
					MethodInfo getMethod = this.prop.GetGetMethod(true);
					bool getMethodOk = getMethod != null && !getMethod.IsStatic && getMethod.GetParameters().Length == 0;
					if (getMethodOk)
					{
						this.getter = (Func<float>)Delegate.CreateDelegate(typeof(Func<float>), this.target, getMethod);
					}
				}
			}
			catch
			{
				this.getter = null;
			}
			try
			{
				bool canFastUpdate = this.target != null && this.updateMethod != null && !this.updateMethod.IsStatic;
				if (canFastUpdate)
				{
					ParameterInfo[] parameters = this.updateMethod.GetParameters();
					bool updateMethodOk = parameters.Length == 1 && parameters[0].ParameterType == typeof(float);
					if (updateMethodOk)
					{
						this.updateAction = (Action<float>)Delegate.CreateDelegate(typeof(Action<float>), this.target, this.updateMethod);
					}
				}
			}
			catch
			{
				this.updateAction = null;
			}
			try
			{
				bool canFastSet = this.target != null && this.prop != null && this.prop.CanWrite && this.prop.PropertyType == typeof(float);
				if (canFastSet)
				{
					MethodInfo setMethod = this.prop.GetSetMethod(true);
					bool setMethodOk = setMethod != null && !setMethod.IsStatic && setMethod.GetParameters().Length == 1;
					if (setMethodOk)
					{
						this.setter = (Action<float>)Delegate.CreateDelegate(typeof(Action<float>), this.target, setMethod);
					}
				}
			}
			catch
			{
				this.setter = null;
			}
		}

		// Token: 0x06000004 RID: 4 RVA: 0x00002178 File Offset: 0x00000378
		public float Get()
		{
			bool flag = this.target == null || (this.prop == null && this.getter == null);
			float result;
			if (flag)
			{
				result = 0f;
			}
			else
			{
				float num;
				if (this.getter != null)
				{
					num = this.getter();
				}
				else
				{
					num = (float)this.prop.GetValue(this.target, null);
				}
				result = ((float.IsNaN(num) || float.IsInfinity(num)) ? 0f : num);
			}
			return result;
		}

		// Token: 0x06000005 RID: 5 RVA: 0x000021E0 File Offset: 0x000003E0
		public void Set(float val)
		{
			bool flag = this.target == null;
			if (!flag)
			{
				bool flag2 = float.IsNaN(val) || float.IsInfinity(val);
				if (flag2)
				{
					val = 0f;
				}
				bool flag3 = this.updateAction != null;
				if (flag3)
				{
					this.updateAction(val);
				}
				else
				{
					bool flag3b = this.updateMethod != null;
					if (flag3b)
					{
						this.invokeArgs[0] = val;
						this.updateMethod.Invoke(this.target, this.invokeArgs);
					}
				}
				bool flag4 = this.setter != null;
				if (flag4)
				{
					this.setter(val);
				}
				else
				{
					bool flag4b = this.prop != null && this.prop.CanWrite;
					if (flag4b)
					{
						this.prop.SetValue(this.target, val, null);
					}
				}
			}
		}

		// Token: 0x04000002 RID: 2
		private object target;

		// Token: 0x04000003 RID: 3
		private PropertyInfo prop;

		// Token: 0x04000004 RID: 4
		private MethodInfo updateMethod;

		// Token: 0x04000005 RID: 5
		private object[] invokeArgs = new object[1];

		private Func<float> getter;

		private Action<float> setter;

		private Action<float> updateAction;
	}
}
