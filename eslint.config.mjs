import js from "@eslint/js";
import { defineConfig } from "eslint/config";
import tseslint from "typescript-eslint";
import stylistic from "@stylistic/eslint-plugin";

export default defineConfig(
	{
		ignores: [
			"**/.vscode-test",
			"**/client/out",
            "**/docs/_build",
            "**/.venv"
		]
	},
	js.configs.recommended,
	...tseslint.configs.recommended,
	...tseslint.configs.stylistic,
	{
		plugins: {
			"@stylistic": stylistic
		},
		rules: {
			"curly": "warn",
			"@stylistic/semi": ["warn", "always"],
			"@typescript-eslint/no-empty-function": "off",
			"@typescript-eslint/naming-convention": [
				"warn",
				{
					"selector": "import",
					"format": ["camelCase", "PascalCase"]
				}
			],
			"@typescript-eslint/no-unused-vars": [
				"error",
				{
					"argsIgnorePattern": "^_"
				}
			]
		}
	}
);
