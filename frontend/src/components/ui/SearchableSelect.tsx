import { NotebookSelect } from './NotebookSelect';

interface SearchableSelectProps {
  value: string;
  options: { value: string; label: string }[];
  onChange: (value: string) => void;
  label: string;
}

export function SearchableSelect({ value, options, onChange, label }: SearchableSelectProps) {
  return <NotebookSelect
    value={value}
    options={options}
    onChange={onChange}
    label={label}
    placeholder={label}
    searchable
    className="w-full"
  />;
}
