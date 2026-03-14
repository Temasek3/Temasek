<template>
    <UApp>
        <ClerkLoaded>
            <Show when="signed-out">
                <div class="flex min-h-screen items-center justify-center bg-orange-50 p-4">
                    <SignIn />
                </div>
            </Show>

            <Show when="signed-in">
                <NuxtLayout>
                    <NuxtPage />
                </NuxtLayout>
            </Show>
        </ClerkLoaded>
    </UApp>
</template>
