using AndroidX.Navigation;
using AndroidX.Navigation.Fragment;
using Kotlin.Jvm;
using Kotlin.Reflect;

namespace NearShare.Utils;

internal static class NavigationExtensions
{
    extension(Fragment fragment)
    {
        public NavController NavController => AndroidX.Navigation.Fragment.FragmentKt.FindNavController(fragment);
    }

    static Dictionary<IKType, NavType> EmptyTypeMap() => [];

    extension(NavController navController)
    {
        public NavGraph CreateGraph(string startDestination, Action<NavGraphBuilder> builder)
            => NavControllerKt.CreateGraph(navController, startDestination, route: null, (KtAction<NavGraphBuilder>)builder);

        public NavGraph CreateGraph(int id, int startDestination, Action<NavGraphBuilder> builder)
            => NavControllerKt.CreateGraph(navController, id, startDestination, (KtAction<NavGraphBuilder>)builder);

        public NavGraph CreateGraph(Java.Lang.Object startDestination, Action<NavGraphBuilder> builder)
            => NavControllerKt.CreateGraph(navController, startDestination, route: null, EmptyTypeMap(), (KtAction<NavGraphBuilder>)builder);

        public void Navigate(string route, Action<NavOptionsBuilder> options)
            => navController.Navigate(route, (KtAction<NavOptionsBuilder>)options);

        public void Navigate<TRoute>(TRoute route) where TRoute : Java.Lang.Object
            => navController.Navigate(route);
    }

    extension(NavigatorProvider navProvider)
    {
        public TNavigator GetNavigator<TNavigator>() where TNavigator : Navigator
            => (TNavigator)navProvider.GetNavigator(typeof(TNavigator).JavaClass);
    }

    extension(NavGraphBuilder graphBuilder)
    {
        public void Fragment<TFragment>(string route, Action<FragmentNavigatorDestinationBuilder> builder)
            where TFragment : Fragment
        {
            FragmentNavigatorDestinationBuilder destinationBuilder = new(
                graphBuilder.Provider.GetNavigator<FragmentNavigator>(),
                route,
                fragmentClass: typeof(TFragment).KotlinClass
            );
            builder(destinationBuilder);
            graphBuilder.AddDestination((NavDestination)destinationBuilder.Build());
        }

        public void Fragment<TFragment>(int id, Action<FragmentNavigatorDestinationBuilder> builder)
            where TFragment : Fragment
        {
            FragmentNavigatorDestinationBuilder destinationBuilder = new(
                graphBuilder.Provider.GetNavigator<FragmentNavigator>(),
                id,
                fragmentClass: typeof(TFragment).KotlinClass
            );
            builder(destinationBuilder);
            graphBuilder.AddDestination((NavDestination)destinationBuilder.Build());
        }

        public void Fragment<TFragment, TRoute>(Action<FragmentNavigatorDestinationBuilder> builder)
            where TFragment : Fragment
            where TRoute : Java.Lang.Object
        {
            FragmentNavigatorDestinationBuilder destinationBuilder = new(
                graphBuilder.Provider.GetNavigator<FragmentNavigator>(),
                route: typeof(TRoute).KotlinClass,
                typeMap: EmptyTypeMap(),
                fragmentClass: typeof(TFragment).KotlinClass
            );
            builder(destinationBuilder);
            graphBuilder.AddDestination((NavDestination)destinationBuilder.Build());
        }
    }

    extension(NavDestinationBuilder destinationBuilder)
    {
        public void Argument(string name, Action<NavArgumentBuilder> builder)
            => destinationBuilder.Argument(name, (KtAction<NavArgumentBuilder>)builder);
    }

    extension(NavOptions)
    {
        public static NavOptions Create(Action<NavOptionsBuilder> builder)
            => NavOptionsBuilderKt.NavOptions((KtAction<NavOptionsBuilder>)builder);
    }

    extension(NavOptionsBuilder optionsBuilder)
    {
        public void InvokePopUpTo(int id, Action<PopUpToBuilder> builder)
            => optionsBuilder.InvokePopUpTo(id, (KtAction<PopUpToBuilder>)builder);

        public void InvokePopUpTo(string route, Action<PopUpToBuilder> builder)
            => optionsBuilder.InvokePopUpTo(route, (KtAction<PopUpToBuilder>)builder);

        public void InvokePopUpTo(IKClass route, Action<PopUpToBuilder> builder)
            => optionsBuilder.InvokePopUpTo(route, (KtAction<PopUpToBuilder>)builder);

        public void InvokePopUpTo(Java.Lang.Object route, Action<PopUpToBuilder> builder)
            => optionsBuilder.InvokePopUpTo(route, (KtAction<PopUpToBuilder>)builder);
    }
}

static class TypeExtensions
{
    extension(Type type)
    {
        public Java.Lang.Class JavaClass => Java.Lang.Class.FromType(type);
        public IKClass KotlinClass => JvmClassMappingKt.GetKotlinClass(type.JavaClass);
    }

    extension(Java.Lang.Class javaClass)
    {
        public IKClass KotlinClass => JvmClassMappingKt.GetKotlinClass(javaClass);
    }
}

sealed class KtAction<T>(Action<T> action) : Java.Lang.Object, Kotlin.Jvm.Functions.IFunction1
    where T : Java.Lang.Object
{
    public Java.Lang.Object? Invoke(Java.Lang.Object? p0)
    {
        action((T?)p0!);
        return Kotlin.Unit.Instance;
    }

    public static explicit operator KtAction<T>(Action<T> action) => new(action);
}
