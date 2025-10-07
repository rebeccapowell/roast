// Global CSS side-effect imports (like globals.css)
declare module "*.css";

// CSS Module declarations
declare module "*.module.css" {
  const classes: { [key: string]: string };
  export default classes;
}

declare module "*.scss";

declare module "*.module.scss" {
  const classes: { [key: string]: string };
  export default classes;
}
