export interface SocialLink {
  id: 'linkedin' | 'github' | 'twitter' | 'facebook';
  label: string;
  url: string;
}

export const PROFILE = {
  name: 'محمد ممدوح',
  title: 'مهندس برمجيات',
  bio: `حاصل على بكالوريوس هندسة — قسم هندسة الحاسبات والمنظومات.
مهتم بتطوير مواقع الويب، وصممت هذا الموقع لمساعدة طلاب الثانوية العامة على فهم خياراتهم وتوقع كلياتهم بسهولة.`,
  photoSrc: 'assets/mohamed-mamdouh.png',
  socialLinks: [
    {
      id: 'linkedin',
      label: 'LinkedIn',
      url: 'https://www.linkedin.com/in/mohamed-mamdouh-220806192/',
    },
    {
      id: 'github',
      label: 'GitHub',
      url: 'https://github.com/MohamedMamdoouh',
    },
    {
      id: 'facebook',
      label: 'Facebook',
      url: 'https://www.facebook.com/mohamedmamdouh2001/',
    },
    {
      id: 'twitter',
      label: 'X',
      url: 'https://x.com/ellamby33',
    },
  ] satisfies SocialLink[],
};
