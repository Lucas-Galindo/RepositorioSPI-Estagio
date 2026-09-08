import { Icon } from "./Icon";

export function EmptyState({ title, desc }: { title: string; desc: string }) {
  return (
    <div className="empty-state">
      <div className="ic">
        <Icon name="empty" size={20} />
      </div>
      <h4>{title}</h4>
      <p>{desc}</p>
    </div>
  );
}
