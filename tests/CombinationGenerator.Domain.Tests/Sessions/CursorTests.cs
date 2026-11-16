using CombinationGenerator.Domain.Sessions;

namespace CombinationGenerator.Domain.Tests.Sessions;

public class CursorTests
{
    [Fact]
    public void Create_CopiesTheArray_SoLaterChangesDoNotLeakIn()
    {
        int[] source = [1, 2, 3];
        var cursor = Cursor.Create(0, source);

        source[0] = 99;

        Assert.Equal(new[] { 1, 2, 3 }, cursor.Permutation);
    }

    [Fact]
    public void Create_InvalidArguments_Throw()
    {
        Assert.Throws<ArgumentNullException>(() => Cursor.Create(0, null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => Cursor.Create(-1, [1]));
        Assert.Throws<ArgumentException>(() => Cursor.Create(0, []));
    }
}
