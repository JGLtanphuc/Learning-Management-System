import React, { useState } from "react";
import { AcademicPolicyModal, type PolicyType } from "./AcademicPolicyModal";

export interface FooterLinkItem {
  label: string;
  href?: string;
  policyType?: PolicyType;
  onClick?: () => void;
}

interface PortalFooterProps {
  links?: FooterLinkItem[];
  className?: string;
}

const DEFAULT_FOOTER_LINKS: FooterLinkItem[] = [
  { label: "Điều khoản đào tạo", policyType: "TERMS" },
  { label: "Quy chế chuyên cần", policyType: "ATTENDANCE" },
  { label: "Hỗ trợ học viên (Help Desk)", policyType: "HELP_DESK" },
  { label: "Đóng góp ý kiến", policyType: "FEEDBACK" },
];

export function PortalFooter({ links, className = "" }: PortalFooterProps) {
  const [activePolicy, setActivePolicy] = useState<PolicyType | null>(null);

  const displayLinks = links ?? DEFAULT_FOOTER_LINKS;

  const handleLinkClick = (item: FooterLinkItem) => {
    if (item.onClick) {
      item.onClick();
    } else if (item.policyType) {
      setActivePolicy(item.policyType);
    }
  };

  return (
    <>
      <footer
        className={`pt-4 pb-1 border-t border-slate-200/80 dark:border-slate-800 flex flex-col md:flex-row items-center justify-between text-xs text-slate-500 dark:text-slate-400 gap-2.5 text-center md:text-left ${className}`}
      >
        <div className="flex flex-wrap items-center justify-center md:justify-start gap-1.5 sm:gap-2">
          <span className="font-semibold text-slate-700 dark:text-slate-300">
            © 2026 FPT Academy System (FAS)
          </span>
        </div>

        <div className="flex flex-wrap items-center justify-center gap-x-2.5 sm:gap-x-3 gap-y-1 font-normal">
          {displayLinks.map((item, idx) => (
            <React.Fragment key={item.label}>
              {idx > 0 && (
                <span className="text-slate-300 dark:text-slate-700 select-none">
                  ·
                </span>
              )}
              {item.href ? (
                <a
                  href={item.href}
                  className="hover:text-slate-900 dark:hover:text-white transition-colors"
                >
                  {item.label}
                </a>
              ) : (
                <button
                  type="button"
                  onClick={() => handleLinkClick(item)}
                  className="hover:text-slate-900 dark:hover:text-white transition-colors cursor-pointer text-xs"
                >
                  {item.label}
                </button>
              )}
            </React.Fragment>
          ))}
        </div>
      </footer>

      <AcademicPolicyModal
        open={!!activePolicy}
        onOpenChange={(open) => !open && setActivePolicy(null)}
        policyType={activePolicy}
      />
    </>
  );
}

export default PortalFooter;
