﻿using BrightstarDB.EntityFramework;

namespace BrightstarDB.Tests.EntityFramework
{
    [Entity]
    public interface IChildEntity2
    {
        [Identifier("http://example.org/repro/", KeyProperties = new[] { "Parent", "Code" })]
        string Id
        {
            get;
        }

        string Code
        {
            get;
            set;
        }

        string Description
        {
            get;
            set;
        }

        IParentEntity2 Parent
        {
            get;
            set;
        }
    }
}