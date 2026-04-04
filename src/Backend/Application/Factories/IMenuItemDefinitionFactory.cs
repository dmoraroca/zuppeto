using YepPet.Application.Admin;
using YepPet.Domain.Navigation;

namespace YepPet.Application.Factories;

public interface IMenuItemDefinitionFactory
{
    MenuItemDefinition Create(SaveMenuRequest request);
}
