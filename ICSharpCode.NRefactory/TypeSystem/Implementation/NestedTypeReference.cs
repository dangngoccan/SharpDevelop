﻿// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <author name="Daniel Grunwald"/>
//     <version>$Revision$</version>
// </file>
using System;

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	/// <summary>
	/// Type reference used to reference nested types.
	/// </summary>
	public class NestedTypeReference : AbstractTypeReference
	{
		ITypeReference baseTypeRef;
		string name;
		int additionalTypeParameterCount;
		
		/// <summary>
		/// Creates a new NestedTypeReference.
		/// </summary>
		/// <param name="baseTypeRef">Reference to the base type.</param>
		/// <param name="name"></param>
		/// <param name="additionalTypeParameterCount"></param>
		public NestedTypeReference(ITypeReference baseTypeRef, string name, int additionalTypeParameterCount)
		{
			if (baseTypeRef == null)
				throw new ArgumentNullException("baseTypeRef");
			if (name == null)
				throw new ArgumentNullException("name");
			this.baseTypeRef = baseTypeRef;
			this.name = name;
			this.additionalTypeParameterCount = additionalTypeParameterCount;
		}
		
		public override IType Resolve(ITypeResolveContext context)
		{
			IType baseType = baseTypeRef.Resolve(context);
			int tpc = baseType.TypeParameterCount;
			foreach (IType type in baseType.GetNestedTypes(context)) {
				if (type.Name == name && type.TypeParameterCount == tpc + additionalTypeParameterCount)
					return type;
			}
			return SharedTypes.UnknownType;
		}
		
		public override string ToString()
		{
			if (additionalTypeParameterCount == 0)
				return baseTypeRef + "+" + name;
			else
				return baseTypeRef + "+" + name + "`" + additionalTypeParameterCount;
		}
	}
}
