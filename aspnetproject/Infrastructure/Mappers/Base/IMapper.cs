namespace aspnetproject.BusinessLogic.Mappers.Base;

public interface IMapper<TEntity, in TCreate, out TDisplay>
{
    public TEntity  ToEntity(TCreate displayDto);
    public TDisplay ToDisplay(TEntity entity);
}