﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace Confuser.Protections.TypeScramble.Scrambler.Rewriter.Instructions {
	internal sealed class MemberRefInstructionRewriter : InstructionRewriter<MemberRef> {

		internal override void ProcessOperand(TypeService service, MethodDef method, IList<Instruction> body, ref int index, MemberRef operand) {
			Debug.Assert(service != null, $"{nameof(service)} != null");
			Debug.Assert(method != null, $"{nameof(method)} != null");
			Debug.Assert(body != null, $"{nameof(body)} != null");
			Debug.Assert(operand != null, $"{nameof(operand)} != null");
			Debug.Assert(index >= 0, $"{nameof(index)} >= 0");
			Debug.Assert(index < body.Count, $"{nameof(index)} < {nameof(body)}.Count");

			var current = service.GetItem(method);

			if (operand.MethodSig == null)
				return;

			if (operand.MethodSig.Params.Count > 0 || body[index].OpCode != OpCodes.Newobj)
				return;

			ModuleDef mod = method.Module;

			var gettype = typeof(Type).GetMethod("GetTypeFromHandle");
			var createInstance = typeof(Activator).GetMethod("CreateInstance", new Type[] { typeof(Type) });

			TypeSig sig = null;

			if (operand.Class is TypeRef typeRef)
				sig = typeRef.ToTypeSig();

			if (operand.Class is TypeSpec typeSpec)
				sig = typeSpec.ToTypeSig();

			if (sig != null) {

				//ScannedItem t = service.GetItem(operand.MDToken);
				//if (t != null) {
				//    sig = t.CreateGenericTypeSig(service.GetItem(method.DeclaringType.MDToken));
				// }
				var paramCount = operand.MethodSig.Params.Count;

				body[index].OpCode = OpCodes.Ldtoken;

				var gen = current?.GetGeneric(sig);
				TypeSpecUser newTypeSpec = null;
				if (gen != null) {
					newTypeSpec = new TypeSpecUser(new GenericMVar(gen.Number));
				}
				else {
					newTypeSpec = new TypeSpecUser(sig);
				}
				body[index].Operand = newTypeSpec;

				/*
				var genericCallSig =  new GenericInstMethodSig( new TypeSig[] { current.ConvertToGenericIfAvalible(sig) });
				foreach(var param in operand.MethodSig.Params.Select(x => current.ConvertToGenericIfAvalible(x))) {
				    genericCallSig.GenericArguments.Add(param);
				}

				// tgtMethod.GenericInstMethodSig = genericCallSig;
				var spec = new MethodSpecUser(tgtMethod, genericCallSig);

				body[index].OpCode = OpCodes.Call;
				body[index].Operand = tgtMethod;
				*/

				body.Insert(++index, Instruction.Create(OpCodes.Call, mod.Import(gettype)));
				body.Insert(++index, Instruction.Create(OpCodes.Call, mod.Import(createInstance)));
			}
		}
	}
}
