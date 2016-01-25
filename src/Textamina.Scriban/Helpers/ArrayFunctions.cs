﻿// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license. See license.txt file in the project root for full license information.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Textamina.Scriban.Parsing;
using Textamina.Scriban.Runtime;

namespace Textamina.Scriban.Helpers
{
    /// <summary>
    /// Array functions available through the object 'array' in scriban.
    /// </summary>
    public static class ArrayFunctions
    {
        public static string Join(string delimiter, IEnumerable enumerable)
        {
            var text = new StringBuilder();
            bool afterFirst = false;
            foreach (var obj in enumerable)
            {
                if (afterFirst)
                {
                    text.Append(delimiter);
                }
                // TODO: We need to convert to a string but we don't have a span!
                text.Append(ScriptValueConverter.ToString(new SourceSpan("unknown", new TextPosition(), new TextPosition()), obj));
                afterFirst = true;
            }
            return text.ToString();
        }

        public static IEnumerable Uniq(IEnumerable iterator)
        {
            if (iterator == null)
            {
                return Enumerable.Empty<object>();
            }

            var set = new SortedSet<object>();
            foreach (var item in iterator)
            {
                set.Add(item);
            }
            return set;
        }

        public static int Size(IEnumerable list)
        {
            var collection = list as ICollection;
            if (collection != null)
            {
                return collection.Count;
            }

            // Slow path, go through the whole list
            return list.Cast<object>().Count();
        }

        public static object First(IEnumerable iterator)
        {
            if (iterator == null)
            {
                return null;
            }

            var list = iterator as IList;
            if (list != null)
            {
                if (list.Count > 0)
                {
                    return list[0];
                }
                return null;
            }

            foreach (var item in iterator)
            {
                return item;
            }

            return null;
        }

        public static object Last(IEnumerable iterator)
        {
            if (iterator == null)
            {
                return null;
            }

            var list = iterator as IList;
            if (list != null)
            {
                if (list.Count > 0)
                {
                    return list[list.Count - 1];
                }
                return null;
            }

            // Slow path, go through the whole list
            return iterator.Cast<object>().LastOrDefault();
        }

        public static IEnumerable Reverse(IEnumerable iterator)
        {
            if (iterator == null)
            {
                return null;
            }

            // TODO: provide a special path for IList
            //var list = iterator as IList;
            //if (list != null)
            //{
            //}
            return iterator.Cast<object>().Reverse();
        }

        public static IList RemoveAt(int index, IList list)
        {
            if (list == null)
            {
                return null;
            }

            if (index >= 0 && index < list.Count)
            {
                list.RemoveAt(index);
            }
            return list;
        }

        public static IList Add(object value, IList list)
        {
            if (list == null)
            {
                return null;
            }

            list.Add(value);
            return list;
        }

        public static IList AddRange(IEnumerable iterator, IList list)
        {
            if (list == null)
            {
                return null;
            }

            if (iterator != null)
            {
                foreach (var value in iterator)
                {
                    list.Add(value);
                }
            }

            return list;
        }


        public static IList InsertAt(int index, object value, IList list)
        {
            if (list == null)
            {
                return null;
            }

            if (index < 0)
            {
                index = 0;
            }

            // Make sure that the list has already inserted elements before the index
            for (int i = list.Count; i < index; i++)
            {
                list.Add(null);
            }

            list.Insert(index, value);

            return list;
        }

        [ScriptFunctionIgnore]
        public static IEnumerable Sort(TemplateContext context, object input, string member = null)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (input == null)
            {
                return Enumerable.Empty<object>();
            }

            var enumerable = input as IEnumerable;
            if (enumerable == null)
            {
                return new List<object>(1) {input };
            }

            var list = enumerable.Cast<object>().ToList();
            if (list.Count == 0)
                return list;

            if (string.IsNullOrEmpty(member))
            {
                list.Sort();
            }
            else
            {
                list.Sort((a, b) =>
                {
                    // TODO: Check accessors
                    var leftAccessor = context.GetMemberAccessor(a);
                    var rightAccessor = context.GetMemberAccessor(b);

                    return Comparer<object>.Default.Compare(leftAccessor?.GetValue(a, member),
                        rightAccessor?.GetValue(b, member));
                });
            }

            return list;
        }

        [ScriptFunctionIgnore]
        public static IEnumerable Map(TemplateContext context, object input, string member)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (member == null) throw new ArgumentNullException(nameof(member));
            if (input == null)
            {
                yield break;
            }

            var enumerable = input as IEnumerable;
            var list = enumerable?.Cast<object>().ToList() ?? new List<object>(1) {input};
            if (list.Count == 0)
            {
                yield break;
            }

            foreach (var item in list)
            {
                var itemAccessor = context.GetMemberAccessor(item);
                if (itemAccessor.HasMember(item, member))
                {
                    yield return itemAccessor.GetValue(item, member);
                }
            }
        }

        /// <summary>
        /// Registers the builtins provided by this class to the specified <see cref="ScriptObject"/>.
        /// </summary>
        /// <param name="builtins">The builtins object.</param>
        /// <exception cref="System.ArgumentNullException">If builtins is null</exception>
        [ScriptFunctionIgnore]
        public static void Register(ScriptObject builtins)
        {
            if (builtins == null) throw new ArgumentNullException(nameof(builtins));
            var arrayObject = ScriptObject.From(typeof (ArrayFunctions));
            arrayObject.SetValue("sort", new DelegateCustomFunction(Sort), true);
            arrayObject.SetValue("map", new DelegateCustomFunction(Map), true);

            builtins.SetValue("array", arrayObject, true);
        }

        private static object Sort(TemplateContext context, ScriptNode callerContext, ScriptArray parameters)
        {
            if (parameters.Count < 1 || parameters.Count > 2)
            {
                throw new ScriptRuntimeException(callerContext.Span, $"Unexpected number of arguments [{parameters.Count}] for sort. Expecting at least 1 parameter <property>? <array>");
            }

            var target = parameters[parameters.Count - 1];
            string member = null;
            if (parameters.Count == 2)
            {
               member = ScriptValueConverter.ToString(callerContext.Span, parameters[0]);
            }

            return Sort(context, target, member);
        }

        private static object Map(TemplateContext context, ScriptNode callerContext, ScriptArray parameters)
        {
            if (parameters.Count != 2)
            {
                throw new ScriptRuntimeException(callerContext.Span, $"Unexpected number of arguments [{parameters.Count}] for map. Expecting at 2 parameters: <property> <array>");
            }

            var member = ScriptValueConverter.ToString(callerContext.Span, parameters[0]);
            var target = parameters[1];

            return Map(context, target, member);
        }

    }
}