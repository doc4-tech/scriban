﻿// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license. See license.txt file in the project root for full license information.
using System.Collections.Generic;

namespace Textamina.Scriban.Runtime
{
    [ScriptSyntax("array initializer", "[item1, item2,...]")]
    public class ScriptArrayInitializerExpression : ScriptExpression
    {
        public ScriptArrayInitializerExpression()
        {
            Values = new List<ScriptExpression>();
        }

        public List<ScriptExpression> Values { get; private set; }

        public override void Evaluate(TemplateContext context)
        {
            var scriptArray = new ScriptArray();
            foreach (var value in Values)
            {
                var valueEval = context.Evaluate(value);
                scriptArray.Add(valueEval);
            }
            context.Result = scriptArray;
        }

        public override string ToString()
        {
            return $"[{string.Join(", ", Values)}]";
        }
    }
}