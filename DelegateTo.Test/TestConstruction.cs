using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;
using DelegateTo.Test;
using DelegateTo;

namespace DelegateTo.Test;
public class TestConstruction
{
    [Fact]
    public void Test1()
    {
        var parent = new Parent { Child = new() { X = 1 }, Child2 = new Child2{ R = 42, }};
        Assert.Equal(1, parent.X);
        Assert.Equal(1, parent.A());
        Assert.Equal(42, parent.R);
        
        var parent2 = new Parent2 { Child = new() { X = 1 }, Child2 = new Child2{ R = 43, } };
        Assert.Equal(1, parent2.X);
        Assert.Equal(1, parent2.A());
        Assert.Equal(43, parent2.R);
    }
    
    
    [Fact]
    public void InlineAttributeShouldBePlacedIfAskedFor()
    {
        var parent = new InlineStruct { Child = new() { X = 1 } };
        Assert.Equal(1, parent.X);
        Assert.Equal(1, parent.A());
        
        var parent2 = new InlineClass { Child = new() { X = 1 } };
        Assert.Equal(1, parent2.X);
        Assert.Equal(1, parent2.A());

        var method = typeof(InlineStruct).GetMethod(nameof(InlineStruct.A));
        Assert.NotNull(method);
        Assert.True((method.MethodImplementationFlags & MethodImplAttributes.AggressiveInlining) != 0);
        
        method = typeof(InlineClass).GetMethod(nameof(InlineClass.A));
        Assert.NotNull(method);
        Assert.True((method.MethodImplementationFlags & MethodImplAttributes.AggressiveInlining) != 0);
        
        method = typeof(Parent).GetMethod(nameof(Parent.A));
        Assert.NotNull(method);
        // not inlined!
        Assert.True((method.MethodImplementationFlags & MethodImplAttributes.AggressiveInlining) == 0); 
        
        method = typeof(Parent2).GetMethod(nameof(Parent2.A));
        Assert.NotNull(method);
        // not inlined!
        Assert.True((method.MethodImplementationFlags & MethodImplAttributes.AggressiveInlining) == 0); 
        
        method = typeof(InlineStruct).GetMethod("get_" + nameof(InlineStruct.X));
        Assert.NotNull(method);
        Assert.True((method.MethodImplementationFlags & MethodImplAttributes.AggressiveInlining) != 0);
        
        
        method = typeof(InlineClass).GetMethod("get_" + nameof(InlineClass.X));
        Assert.NotNull(method);
        Assert.True((method.MethodImplementationFlags & MethodImplAttributes.AggressiveInlining) != 0);
    }
}


partial struct InlineStruct
{
    [GenerateDelegate(Inline = true)]
    public Child Child { get; set; }
}

partial class InlineClass
{
    [GenerateDelegate(Inline = true)]
    public Child Child { get; set; }
}

partial struct Parent
{
    [GenerateDelegate]
    public Child Child { get; set; }

    [GenerateDelegate]
    public Child2 Child2 { get; set; }
}

public partial class Parent2
{
    [GenerateDelegate]
    public Child Child { get; set; }

    [GenerateDelegate]
    public Child2 Child2 { get; set; }
}

public class Child
{
    public int X { get; set; }
    public int Y { get; }
    private int Z { get; set; }

    public int A() => 1;
}


public class Child2
{
    public int R { get; set; }
    public int Q { get; }
    private int Y { get; set; }
}
