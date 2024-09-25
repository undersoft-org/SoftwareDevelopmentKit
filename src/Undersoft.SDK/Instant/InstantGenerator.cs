namespace Undersoft.SDK.Instant
{
    using Rubrics;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using Undersoft.SDK.Logging;
    using Undersoft.SDK.Proxies;
    using Undersoft.SDK.Series;
    using Undersoft.SDK.Uniques;
    using Undersoft.SDK.Utilities;

    public class InstantGenerator<T> : InstantGenerator
    {
        public InstantGenerator(InstantType modeType = InstantType.Reference)
            : base(typeof(T), modeType) { }

        public InstantGenerator(string createdTypeName, InstantType modeType = InstantType.Reference)
            : base(typeof(T), createdTypeName, modeType) { }
    }

    public class InstantGenerator : IInstantGenerator
    {
        protected MemberBuilderCreator memberBuilderCreator = new MemberBuilderCreator();
        protected ISeries<MemberBuilder> memberBuilders = new Registry<MemberBuilder>();
        private Type compiledType;

        public InstantGenerator(
            IEnumerable<MemberInfo> instantMembers,
            InstantType modeType = InstantType.Reference
        ) : this(instantMembers.ToArray(), null, modeType) { }

        public InstantGenerator(
            IEnumerable<MemberInfo> instantMembers,
            string createdTypeName,
            InstantType modeType = InstantType.Reference
        )
        {
            Name =
                string.IsNullOrEmpty(createdTypeName)
                    ? createdTypeName
                    : "Runtime" + DateTime.Now.ToBinary().ToString();

            Name += "Instant";

            mode = modeType;

            memberBuilderCreator = new MemberBuilderCreator();

            memberBuilders = memberBuilderCreator.Create(memberBuilderCreator.PrepareMembers(instantMembers));

            Rubrics = new MemberRubrics(memberBuilders.Select(m => m.Member).ToArray());
            Rubrics.KeyRubrics = new MemberRubrics();
        }

        public InstantGenerator(
            MemberRubrics instantRubrics,
            string createdTypeName,
            InstantType modeType = InstantType.Reference
        ) : this(instantRubrics.ToArray(), createdTypeName, modeType) { }

        public InstantGenerator(Type baseOnType, InstantType modeType = InstantType.Reference)
            : this(baseOnType, null, modeType) { }

        public InstantGenerator(
            Type baseOnType,
            string createdTypeName,
            InstantType modeType = InstantType.Reference
        )
        {
            BaseType = baseOnType;

            if (modeType == InstantType.Derived)
                IsDerived = true;

            Name = string.IsNullOrEmpty(createdTypeName) ? baseOnType.FullName : createdTypeName;
            Name += "Instant";
            mode = modeType;

            memberBuilderCreator = new MemberBuilderCreator();
            memberBuilders = memberBuilderCreator.Create(baseOnType);

            Rubrics = new MemberRubrics(memberBuilders.Select(m => m.Member).ToArray());
            Rubrics.KeyRubrics = new MemberRubrics();
        }

        public Type BaseType { get; set; }

        public bool IsDerived { get; set; }

        public string Name { get; set; }

        public IRubrics Rubrics { get; set; }

        public int Size { get; set; }

        public Type Type { get; set; }

        private InstantType mode { get; set; }

        private long? _typeId = null;
        private long TypeId => _typeId ??= Type.UniqueKey32();

        public IInstant Generate()
        {
            if (this.Type != null)
                return CreateInstance();

            try
            {
                switch (mode)
                {
                    case InstantType.Reference:
                        CompileBuildedType(
                            new InstantCompilerReferenceTypes(this, memberBuilders)
                        );
                        break;
                    case InstantType.ValueType:
                        CompileBuildedType(new InstantCompilerValueTypes(this, memberBuilders));
                        break;
                    case InstantType.Derived:
                        CompileDerivedType(
                            new InstantCompilerDerivedTypes(this, memberBuilders)
                        );
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                throw new InstantTypeCompilerException(
                    "Instant compilation at runtime failed see inner exception",
                    ex
                );
            }
            return CreateInstance();
        }

        public object New()
        {           
            return Generate();
        }

        private IInstant CreateInstance()
        {
            var figure = (IInstant)this.Type.New();
            //figure.Changes = new HashSet<string>();
            //figure.PropertyChanged += OnPropertyChanged;
            figure.Id = Unique.NewId;
            figure.TypeId = TypeId;
            return figure;
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            ((IInstant)sender).Changes.Add(e.PropertyName);
        }

        private void CompileDerivedType(InstantCompiler compiler)
        {
            var fcdt = compiler;
            compiledType = fcdt.CompileInstantType(Name);
            Rubrics.KeyRubrics.Add(fcdt.Identities.Values);
            Type = compiledType.New().GetType();
            if (!(Rubrics.AsValues().Any(m => m.Name == "code")))
            {
                var f = this.Type.GetField("code", BindingFlags.NonPublic | BindingFlags.Instance);

                if (!Rubrics.TryGet("code", out MemberRubric mr))
                {
                    mr = new MemberRubric(f);
                    mr.InstantField = f;
                    Rubrics.Insert(0, mr);
                }
                mr.RubricName = "code";
            }
            Rubrics.Update();
            try
            {
                Size = Marshal.SizeOf(Type);
            }
            catch (Exception ex)
            {
                this.Warning<Instantlog>("Marshal cannot establish size of type", null, ex);
                Size = Rubrics.BinarySize;
            }
        }

        private void CompileBuildedType(InstantCompiler compiler)
        {
            var fcvt = compiler;
            compiledType = fcvt.CompileInstantType(Name);
            Rubrics.KeyRubrics.Add(fcvt.Identities.Values);
            Type = compiledType.New().GetType();
            Rubrics.Update();
            try
            {
                Size = Marshal.SizeOf(Type);
            }
            catch (Exception ex)
            {
                this.Warning<Instantlog>("Marshal cannot establish size of type", null, ex);
                Size = Rubrics.BinarySize;
            }
        }

        private MemberRubric[] CreateMemberRurics(IList<MemberInfo> membersInfo)
        {
            return membersInfo
                .Select(
                    m =>
                        !(m is MemberRubric rubric)
                            ? m.MemberType == MemberTypes.Field
                                ? new MemberRubric((FieldInfo)m)
                                : m.MemberType == MemberTypes.Property
                                    ? new MemberRubric((PropertyInfo)m)
                                    : null
                            : rubric
                )
                .Where(p => p != null)
                .ToArray();
        }
    }

    public class InstantTypeCompilerException : Exception
    {
        public InstantTypeCompilerException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
