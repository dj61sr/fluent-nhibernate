using System;
using System.Linq;
using System.Reflection;
using FluentNHibernate.Conventions;
using FluentNHibernate.Conventions.AcceptanceCriteria;
using FluentNHibernate.Conventions.Inspections;
using FluentNHibernate.MappingModel;
using FluentNHibernate.MappingModel.ClassBased;

namespace FluentNHibernate.AutoMap
{
    public class AutoMapColumn : IAutoMapper
    {
        private readonly IConventionFinder conventionFinder;

        public AutoMapColumn(IConventionFinder conventionFinder)
        {
            this.conventionFinder = conventionFinder;
        }

        public bool MapsProperty(PropertyInfo property)
        {
            if (HasExplicitTypeConvention(property))
                return true;

            if (property.CanWrite)
                return IsMappableToColumnType(property);

            return false;
        }

        private bool HasExplicitTypeConvention(PropertyInfo property)
        {
            // todo: clean this up!
            //        What it's doing is finding if there are any IUserType conventions
            //        that would be applied to this property, if there are then we should
            //        definitely automap it. The nasty part is that right now we don't have
            //        a model, so we're having to create a fake one so the convention will
            //        apply to it.
            var conventions = conventionFinder
                .Find<IPropertyConvention>()
                .Where(c =>
                {
                    if (!c.GetType().IsGenericType)
                        return false;
                    if (c.GetType().GetGenericTypeDefinition() != typeof(UserTypeConvention<>))
                        return false;

                    var criteria = new ConcreteAcceptanceCriteria<IPropertyInspector>();

                    c.Accept(criteria);

                    return criteria.Matches(new PropertyInspector(new PropertyMapping
                    {
                        Type = new TypeReference(property.DeclaringType),
                        PropertyInfo = property
                    }));
                });

            return conventions.FirstOrDefault() != null;
        }

        private static bool IsMappableToColumnType(PropertyInfo property)
        {
            return property.PropertyType.Namespace == "System"
                   || property.PropertyType.FullName == "System.Drawing.Bitmap";
        }

        public void Map(ClassMapping classMap, PropertyInfo property)
        {
            if (property.DeclaringType != classMap.Type)
                return;

            classMap.AddProperty(GetPropertyMapping(classMap.Type, property));
        }

        public void Map(JoinedSubclassMapping classMap, PropertyInfo property)
        {
            if (property.DeclaringType != classMap.Type)
                return;

            classMap.AddProperty(GetPropertyMapping(classMap.Type, property));
        }

        public void Map(SubclassMapping classMap, PropertyInfo property)
        {
            if (property.DeclaringType != classMap.Type)
                return;

            classMap.AddProperty(GetPropertyMapping(classMap.Type, property));
        }

        private PropertyMapping GetPropertyMapping(Type type, PropertyInfo property)
        {
            var mapping = new PropertyMapping
            {
                ContainingEntityType = type,
                PropertyInfo = property
            };

            mapping.AddDefaultColumn(new ColumnMapping { Name = mapping.PropertyInfo.Name });

            if (!mapping.Attributes.IsSpecified(x => x.Name))
                mapping.Name = mapping.PropertyInfo.Name;

            if (!mapping.Attributes.IsSpecified(x => x.Type))
                mapping.Attributes.SetDefault(x => x.Type, new TypeReference(mapping.PropertyInfo.PropertyType));

            return mapping;
        }
    }
}