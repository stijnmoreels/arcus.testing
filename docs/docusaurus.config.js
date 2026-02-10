const lightCodeTheme = require('./src/prism/light');
const darkCodeTheme = require('./src/prism/dark');

/** @type {import('@docusaurus/types').DocusaurusConfig} */
module.exports = {
  title: 'Arcus - Testing',
  url: 'https://testing.arcus-azure.net/',
  baseUrl: '/',
  onBrokenLinks: 'throw',
  onBrokenMarkdownLinks: 'warn',
  favicon: 'img/favicon.ico',
  organizationName: 'arcus-azure', // Usually your GitHub org/user name.
  projectName: 'Arcus - Testing', // Usually your repo name.
  themeConfig: {
    image: 'img/arcus.jpg',
    navbar: {
      title: '',
      logo: {
        alt: 'Arcus',
        src: 'img/arcus.png',
        srcDark: 'img/arcus_for_dark.png',
      },
      items: [
        {
          type: 'dropdown',
          label: 'Arcus Testing',
          position: 'left',
          items: [
            {
              label: 'Arcus Messaging',
              href: 'https://messaging.arcus-azure.net/',
            },
            {
              label: 'Arcus Observability',
              href: 'https://observability.arcus-azure.net/'
            },
            {
              label: 'Arcus Security',
              href: 'https://security.arcus-azure.net/'
            },
            {
              label: 'Arcus Scripting',
              href: 'https://scripting.arcus-azure.net/'
            }
          ]
        },
        {
          type: 'docsVersionDropdown',

          //// Optional
          position: 'right',
          // Add additional dropdown items at the beginning/end of the dropdown.
          dropdownItemsBefore: [],
          // Do not add the link active class when browsing docs.
          dropdownActiveClassDisabled: true,
          docsPluginId: 'default',
        },
        {
          type: 'search',
          position: 'right',
        },
        {
          href: 'https://github.com/arcus-azure/arcus.testing',
          label: 'GitHub',
          position: 'right',
        },
      ],
    },
    footer: {
      style: 'dark',
      links: [
        {
          title: 'Community',
          items: [
            {
              label: 'Github',
              href: 'https://github.com/arcus-azure/arcus.testing',
            },
            {
              label: 'Contribution guide',
              href: 'https://github.com/arcus-azure/arcus.testing/blob/main/CONTRIBUTING.md'
            },
            {
              label: 'Create GitHub issue',
              href: 'https://github.com/arcus-azure/arcus.testing/issues/new/choose'
            }
          ]
        },
        {
          title: 'Tech-independent',
          items: [
            {
              label: 'Core infrastructure',
              to: '/features/core'
            },
            {
              label: 'Assertions',
              to: '/features/assertion'
            },
            {
              label: 'Logging',
              to: '/features/logging'
            }
          ]
        },
        {
          title: 'Azure',
          items: [
            {
              label: 'Storage account',
              to: '/features/azure/storage/storage-account'
            },
            {
              label: 'Cosmos DB',
              to: '/features/azure/storage/cosmos'
            },
            {
              label: 'Service Bus',
              to: '/features/azure/messaging/servicebus'
            },
            {
              label: 'Data Factory',
              to: '/features/azure/integration/data-factory'
            }
          ]
        },
        {
          title: 'Guidance',
          items: [
            {
              label: 'Migrate Testing Framework to v1',
              to: '/guidance/migrate-from-testing-framework-to-arcus-testing-v1.0'
            },
            {
              label: 'Migrate v1 to v2',
              to: '/guidance/migrate-from-v1-to-v2'
            }
          ]
        }
      ],
      copyright: `Copyright © ${new Date().getFullYear()}, Arcus - Testing maintained by arcus-azure`,
    },
    prism: {
      theme: lightCodeTheme,
      darkTheme: darkCodeTheme,
      additionalLanguages: ['csharp', 'fsharp', 'diff', 'json', 'powershell'],
    },
  },
  presets: [
    [
      '@docusaurus/preset-classic',
      {
        docs: {
          sidebarPath: require.resolve('./sidebars.js'),
          routeBasePath: '/',
          path: 'preview',
          sidebarCollapsible: false,
          // Please change this to your repo.
          editUrl: 'https://github.com/arcus-azure/arcus.testing/edit/main/docs',
          includeCurrentVersion: process.env.CONTEXT !== 'production',
          admonitions: {
            keywords: ['praise'],
            extendDefaults: true,
          }
        },
        theme: {
          customCss: require.resolve('./src/css/custom.css'),
        },
      },
    ],
  ],
  stylesheets: [
    'https://fonts.googleapis.com/css2?family=Bitter:wght@700&family=Inter:wght@400;500&display=swap'
  ],
};
